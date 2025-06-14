#include <Mile.Mobility.Portable.Types.h>
#include <Mile.HyperV.VMBus.h>

#include <cpuid.h>

#include <cstdio>
#include <cstring>

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

    std::printf("%s say hello to you!\n", "HvGin");
    return 0;
}
