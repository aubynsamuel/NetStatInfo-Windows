using System.Net;
using System.Runtime.InteropServices;
using NetStatInfoWin.Models;

namespace NetStatInfoWin.Services;

internal sealed class OwnedConnectionTableReader
{
    private const int AddressFamilyInterNetwork = 2;
    private const int AddressFamilyInterNetworkV6 = 23;
    private const uint ErrorInsufficientBuffer = 122;

    public IReadOnlyList<OwnedConnectionRecord> ReadAllConnections()
    {
        List<OwnedConnectionRecord> rows = new();
        rows.AddRange(ReadTcpConnections(AddressFamilyInterNetwork, TcpTableClass.TcpTableOwnerPidAll));
        rows.AddRange(ReadTcpConnections(AddressFamilyInterNetworkV6, TcpTableClass.TcpTableOwnerPidAll));
        rows.AddRange(ReadUdpConnections(AddressFamilyInterNetwork, UdpTableClass.UdpTableOwnerPid));
        rows.AddRange(ReadUdpConnections(AddressFamilyInterNetworkV6, UdpTableClass.UdpTableOwnerPid));
        return rows;
    }

    private static IEnumerable<OwnedConnectionRecord> ReadTcpConnections(int addressFamily, TcpTableClass tableClass)
    {
        IntPtr buffer = IntPtr.Zero;

        try
        {
            int bufferSize = 0;
            uint result = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, addressFamily, tableClass, 0);
            if (result != ErrorInsufficientBuffer || bufferSize <= 0)
            {
                yield break;
            }

            buffer = Marshal.AllocHGlobal(bufferSize);
            result = GetExtendedTcpTable(buffer, ref bufferSize, true, addressFamily, tableClass, 0);
            if (result != 0)
            {
                yield break;
            }

            int rowCount = Marshal.ReadInt32(buffer);
            IntPtr rowPointer = IntPtr.Add(buffer, sizeof(int));

            if (addressFamily == AddressFamilyInterNetwork)
            {
                int rowSize = Marshal.SizeOf<MibTcpRowOwnerPid>();
                for (int index = 0; index < rowCount; index++)
                {
                    MibTcpRowOwnerPid row = Marshal.PtrToStructure<MibTcpRowOwnerPid>(rowPointer);
                    rowPointer = IntPtr.Add(rowPointer, rowSize);

                    if (row.OwningPid == 0 || row.State == (uint)TcpStateNative.DeleteTcb)
                    {
                        continue;
                    }

                    string localEndpoint = $"{new IPAddress(BitConverter.GetBytes(row.LocalAddr))}:{ConvertPort(row.LocalPort)}";
                    string remoteEndpoint = IsZeroAddress(row.RemoteAddr) && row.RemotePort == 0
                        ? string.Empty
                        : $"{new IPAddress(BitConverter.GetBytes(row.RemoteAddr))}:{ConvertPort(row.RemotePort)}";

                    yield return new OwnedConnectionRecord(unchecked((int)row.OwningPid), ConnectionProtocol.Tcp, localEndpoint, remoteEndpoint);
                }
            }
            else
            {
                int rowSize = Marshal.SizeOf<MibTcp6RowOwnerPid>();
                for (int index = 0; index < rowCount; index++)
                {
                    MibTcp6RowOwnerPid row = Marshal.PtrToStructure<MibTcp6RowOwnerPid>(rowPointer);
                    rowPointer = IntPtr.Add(rowPointer, rowSize);

                    if (row.OwningPid == 0 || row.State == (uint)TcpStateNative.DeleteTcb)
                    {
                        continue;
                    }

                    string localEndpoint = $"{new IPAddress(row.LocalAddr, ConvertScopeId(row.LocalScopeId))}:{ConvertPort(row.LocalPort)}";
                    string remoteEndpoint = IsZeroAddress(row.RemoteAddr) && row.RemotePort == 0
                        ? string.Empty
                        : $"{new IPAddress(row.RemoteAddr, ConvertScopeId(row.RemoteScopeId))}:{ConvertPort(row.RemotePort)}";

                    yield return new OwnedConnectionRecord(unchecked((int)row.OwningPid), ConnectionProtocol.Tcp, localEndpoint, remoteEndpoint);
                }
            }
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

    private static IEnumerable<OwnedConnectionRecord> ReadUdpConnections(int addressFamily, UdpTableClass tableClass)
    {
        IntPtr buffer = IntPtr.Zero;

        try
        {
            int bufferSize = 0;
            uint result = GetExtendedUdpTable(IntPtr.Zero, ref bufferSize, true, addressFamily, tableClass, 0);
            if (result != ErrorInsufficientBuffer || bufferSize <= 0)
            {
                yield break;
            }

            buffer = Marshal.AllocHGlobal(bufferSize);
            result = GetExtendedUdpTable(buffer, ref bufferSize, true, addressFamily, tableClass, 0);
            if (result != 0)
            {
                yield break;
            }

            int rowCount = Marshal.ReadInt32(buffer);
            IntPtr rowPointer = IntPtr.Add(buffer, sizeof(int));

            if (addressFamily == AddressFamilyInterNetwork)
            {
                int rowSize = Marshal.SizeOf<MibUdpRowOwnerPid>();
                for (int index = 0; index < rowCount; index++)
                {
                    MibUdpRowOwnerPid row = Marshal.PtrToStructure<MibUdpRowOwnerPid>(rowPointer);
                    rowPointer = IntPtr.Add(rowPointer, rowSize);

                    if (row.OwningPid == 0)
                    {
                        continue;
                    }

                    string localEndpoint = $"{new IPAddress(BitConverter.GetBytes(row.LocalAddr))}:{ConvertPort(row.LocalPort)}";
                    yield return new OwnedConnectionRecord(unchecked((int)row.OwningPid), ConnectionProtocol.Udp, localEndpoint, null);
                }
            }
            else
            {
                int rowSize = Marshal.SizeOf<MibUdp6RowOwnerPid>();
                for (int index = 0; index < rowCount; index++)
                {
                    MibUdp6RowOwnerPid row = Marshal.PtrToStructure<MibUdp6RowOwnerPid>(rowPointer);
                    rowPointer = IntPtr.Add(rowPointer, rowSize);

                    if (row.OwningPid == 0)
                    {
                        continue;
                    }

                    string localEndpoint = $"{new IPAddress(row.LocalAddr, ConvertScopeId(row.LocalScopeId))}:{ConvertPort(row.LocalPort)}";
                    yield return new OwnedConnectionRecord(unchecked((int)row.OwningPid), ConnectionProtocol.Udp, localEndpoint, null);
                }
            }
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

    private static long ConvertScopeId(uint scopeId)
    {
        return IPAddress.NetworkToHostOrder(unchecked((int)scopeId));
    }

    private static int ConvertPort(uint rawPort)
    {
        return (ushort)IPAddress.NetworkToHostOrder(unchecked((short)rawPort));
    }

    private static bool IsZeroAddress(uint address)
    {
        return address == 0;
    }

    private static bool IsZeroAddress(byte[] address)
    {
        return address.All(static value => value == 0);
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(
        IntPtr tcpTable,
        ref int tcpTableLength,
        bool sort,
        int ipVersion,
        TcpTableClass tableClass,
        uint reserved);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedUdpTable(
        IntPtr udpTable,
        ref int udpTableLength,
        bool sort,
        int ipVersion,
        UdpTableClass tableClass,
        uint reserved);

    private enum TcpTableClass
    {
        TcpTableBasicListener,
        TcpTableBasicConnections,
        TcpTableBasicAll,
        TcpTableOwnerPidListener,
        TcpTableOwnerPidConnections,
        TcpTableOwnerPidAll,
    }

    private enum UdpTableClass
    {
        UdpTableBasic,
        UdpTableOwnerPid,
        UdpTableOwnerModule,
    }

    private enum TcpStateNative
    {
        Closed = 1,
        Listen = 2,
        SynSent = 3,
        SynReceived = 4,
        Established = 5,
        FinWait1 = 6,
        FinWait2 = 7,
        CloseWait = 8,
        Closing = 9,
        LastAck = 10,
        TimeWait = 11,
        DeleteTcb = 12,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcpRowOwnerPid
    {
        public uint State;
        public uint LocalAddr;
        public uint LocalPort;
        public uint RemoteAddr;
        public uint RemotePort;
        public uint OwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibTcp6RowOwnerPid
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] LocalAddr;

        public uint LocalScopeId;
        public uint LocalPort;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] RemoteAddr;

        public uint RemoteScopeId;
        public uint RemotePort;
        public uint State;
        public uint OwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibUdpRowOwnerPid
    {
        public uint LocalAddr;
        public uint LocalPort;
        public uint OwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MibUdp6RowOwnerPid
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] LocalAddr;

        public uint LocalScopeId;
        public uint LocalPort;
        public uint OwningPid;
    }
}
