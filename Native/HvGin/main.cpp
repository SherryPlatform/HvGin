#define MILE_MOBILITY_ENABLE_MINIMUM_SAL
#include <Mile.Mobility.Portable.Types.h>
#include <Mile.HyperV.VMBus.h>
#include <Mile.Helpers.CppBase.h>

#include <unistd.h>
#include <errno.h>
#include <sys/mount.h>
#include <sys/socket.h>
#include <linux/vm_sockets.h>

#include <cpuid.h>

#include <cstdio>
#include <cstring>

EXTERN_C int MOAPI HvGinMountHcsPlan9ReadOnlyShare(
    _In_ MO_UINT32 Port,
    _In_ MO_CONSTANT_STRING AccessName,
    _In_ MO_CONSTANT_STRING MountPoint)
{
    int ErrorCode = 0;

    int Socket = ::socket(AF_VSOCK, SOCK_STREAM, 0);
    if (-1 != Socket)
    {
        sockaddr_vm SocketAddress = { 0 };
        SocketAddress.svm_family = AF_VSOCK;
        SocketAddress.svm_port = Port;
        SocketAddress.svm_cid = VMADDR_CID_HOST;
        if (0 == ::connect(
            Socket,
            reinterpret_cast<sockaddr*>(&SocketAddress),
            sizeof(SocketAddress)))
        {
            int SendBufferSize = 65536;
            if (0 == ::setsockopt(
                Socket,
                SOL_SOCKET,
                SO_SNDBUF,
                &SendBufferSize,
                sizeof(SendBufferSize)))
            {
                if (0 != ::mount(
                    AccessName,
                    MountPoint,
                    "9p",
                    MS_RDONLY,
                    Mile::FormatString(
                        "trans=fd,rfdno=%d,wfdno=%d,msize=%d,noload,aname=%s",
                        Socket,
                        Socket,
                        SendBufferSize,
                        AccessName).c_str()))
                {
                    ErrorCode = errno;
                }
            }
            else
            {
                ErrorCode = errno;
            }
            }
        else
        {
            ErrorCode = errno;
        }

        ::close(Socket);
    }
    else
    {
        ErrorCode = errno;
    }

    return ErrorCode;
}

EXTERN_C int MOAPI HvGinUioRegisterDevice(
    _In_ PMO_GUID ClassId)
{
    if (!ClassId)
    {
        return EINVAL;
    }

    int ErrorCode = 0;

    FILE* FileObject = std::fopen(
        "/sys/bus/vmbus/drivers/uio_hv_generic/new_id",
        "w");
    if (FileObject)
    {
        // "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
        const int GuidStringLength = 36;

        int Result = std::fprintf(
            FileObject,
            "%08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x",
            ClassId->Data1,
            ClassId->Data2,
            ClassId->Data3,
            ClassId->Data4[0],
            ClassId->Data4[1],
            ClassId->Data4[2],
            ClassId->Data4[3],
            ClassId->Data4[4],
            ClassId->Data4[5],
            ClassId->Data4[6],
            ClassId->Data4[7]);
        if (Result < 0)
        {
            ErrorCode = errno;
        }
        else if (GuidStringLength != Result)
        {
            ErrorCode = EIO;
        }

        std::fclose(FileObject);
    }
    else
    {
        ErrorCode = errno;
    }

    return ErrorCode;
}

int main()
{
    HV_CPUID_RESULT HvCpuIdResult;
    std::memset(&HvCpuIdResult, 0, sizeof(HV_CPUID_RESULT));
    __cpuid(
        HvCpuIdFunctionHvInterface,
        HvCpuIdResult.Eax,
        HvCpuIdResult.Ebx,
        HvCpuIdResult.Ecx,
        HvCpuIdResult.Edx);
    if (HvMicrosoftHypervisorInterface == HvCpuIdResult.HvInterface.Interface)
    {
        std::printf("HvGin is running on Microsoft Hyper-V!\n");
    }

    int Result = ::HvGinMountHcsPlan9ReadOnlyShare(
        50001,
        "HostDriverStore",
        "/home/mouri/dxguser/HostDriverStore");
    std::printf("HvGinMountHcsPlan9ReadOnlyShare returns %d\n", Result);

    // doas ln -s /home/mouri/dxguser/wsl /usr/lib/wsl
    // mkdir -p /home/mouri/dxguser/wsl
    // ln -s /home/mouri/dxguser/HostDriverStore/FileRepository /home/mouri/dxguser/wsl/drivers
    // mkdir -p /home/mouri/dxguser/HostDriverStore

    std::printf("%s say hello to you!\n", "HvGin");
    return 0;
}
