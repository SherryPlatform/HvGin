#define MILE_MOBILITY_ENABLE_MINIMUM_SAL
#include <Mile.Mobility.Portable.Types.h>
#include <Mile.HyperV.VMBus.h>

#include <unistd.h>
#include <errno.h>
#include <sys/mount.h>
#include <sys/socket.h>
#include <linux/vm_sockets.h>

#include <cpuid.h>

#include <cstdarg>
#include <cstdio>
#include <cstring>

#include <string>

namespace Mile
{
    /**
     * @brief Write formatted data to a onebyte or multibyte string, suggested
     *        encoding with UTF-8.
     * @param Format Format-control string.
     * @param ArgList Pointer to list of optional arguments to be formatted.
     * @return A formatted string if successful, an empty string otherwise.
    */
    std::string VFormatString(
        _In_ char const* const Format,
        _In_ va_list ArgList)
    {
        int Length = 0;

        // Get the length of the format result.
        {
            va_list CurrentArgList;
            va_copy(CurrentArgList, ArgList);
            Length = std::vsnprintf(nullptr, 0, Format, CurrentArgList);
            va_end(CurrentArgList);
        }
        if (Length > 0)
        {
            // Allocate for the format result.
            std::string Buffer;
            Buffer.resize(static_cast<std::size_t>(Length));

            // Format the string.
            {
                va_list CurrentArgList;
                va_copy(CurrentArgList, ArgList);
                Length = std::vsnprintf(
                    &Buffer[0],
                    Buffer.size() + 1,
                    Format,
                    CurrentArgList);
                va_end(CurrentArgList);
            }
            if (Length > 0)
            {
                // If succeed, resize to fit and return result.
                Buffer.resize(static_cast<std::size_t>(Length));
                return Buffer;
            }
        }

        // If failed, return an empty string.
        return std::string();
    }

    /**
     * @brief Write formatted data to a onebyte or multibyte string, suggested
     *        encoding with UTF-8.
     * @param Format Format-control string.
     * @param ... Optional arguments to be formatted.
     * @return A formatted string if successful, an empty string otherwise.
    */
    std::string FormatString(
        _In_ char const* const Format,
        ...)
    {
        va_list ArgList;
        va_start(ArgList, Format);
        std::string Result = Mile::VFormatString(Format, ArgList);
        va_end(ArgList);
        return Result;
    }
}

EXTERN_C int MOAPI HvGinMountPlan9ReadOnlyShareWithHyperVSocket(
    _In_ MO_UINT32 Port,
    _In_ MO_CONSTANT_STRING AccessName,
    _In_ MO_CONSTANT_STRING MountPoint)
{
    bool Success = false;

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
                Success = (0 == ::mount(
                    AccessName,
                    MountPoint,
                    "9p",
                    MS_RDONLY,
                    Mile::FormatString(
                        "trans=fd,rfdno=%d,wfdno=%d,msize=%d,noload,aname=%s",
                        Socket,
                        Socket,
                        SendBufferSize,
                        AccessName).c_str()));
            }
        }

        ::close(Socket);
    }

    return Success ? 0 : errno;
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

    int Result = ::HvGinMountPlan9ReadOnlyShareWithHyperVSocket(
        50001,
        "HostDriverStore",
        "/home/mouri/dxguser/HostDriverStore");
    std::printf("HvGinMountPlan9ReadOnlyShareWithHyperVSocket returns %d\n", Result);

    // doas ln -s /home/mouri/dxguser/wsl /usr/lib/wsl
    // mkdir -p /home/mouri/dxguser/wsl
    // ln -s /home/mouri/dxguser/HostDriverStore/FileRepository /home/mouri/dxguser/wsl/drivers
    // mkdir -p /home/mouri/dxguser/HostDriverStore

    std::printf("%s say hello to you!\n", "HvGin");
    return 0;
}
