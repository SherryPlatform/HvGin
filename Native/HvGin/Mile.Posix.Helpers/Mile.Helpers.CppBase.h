/*
 * PROJECT:   Mouri Internal Library Essentials
 * FILE:      Mile.Helpers.CppBase.h
 * PURPOSE:   Definition for the essential POSIX helper functions
 *
 * LICENSE:   The MIT License
 *
 * MAINTAINER: MouriNaruto (Kenji.Mouri@outlook.com)
 */

#pragma once

#ifndef MILE_POSIX_HELPERS_CPPBASE
#define MILE_POSIX_HELPERS_CPPBASE

#define MILE_MOBILITY_ENABLE_MINIMUM_SAL
#include <Mile.Mobility.Portable.Types.h>

#include <cstdint>
#include <string>
#include <vector>

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
        _In_ va_list ArgList);

    /**
     * @brief Write formatted data to a onebyte or multibyte string, suggested
     *        encoding with UTF-8.
     * @param Format Format-control string.
     * @param ... Optional arguments to be formatted.
     * @return A formatted string if successful, an empty string otherwise.
    */
    std::string FormatString(
        _In_ char const* const Format,
        ...);

    /**
     * @brief Disables C++ class copy construction.
    */
    class DisableCopyConstruction
    {
    protected:

        DisableCopyConstruction() = default;
        ~DisableCopyConstruction() = default;

    private:

        DisableCopyConstruction(
            const DisableCopyConstruction&) = delete;
        DisableCopyConstruction& operator=(
            const DisableCopyConstruction&) = delete;
    };

    /**
     * @brief Disables C++ class move construction.
    */
    class DisableMoveConstruction
    {
    protected:

        DisableMoveConstruction() = default;
        ~DisableMoveConstruction() = default;

    private:

        DisableMoveConstruction(
            const DisableMoveConstruction&&) = delete;
        DisableMoveConstruction& operator=(
            const DisableMoveConstruction&&) = delete;
    };

    /**
     * @brief The template for defining the task when exit the scope.
     * @tparam TaskHandlerType The type of the task handler.
     * @remark For more information, see ScopeGuard.
    */
    template<typename TaskHandlerType>
    class ScopeExitTaskHandler :
        DisableCopyConstruction,
        DisableMoveConstruction
    {
    private:

        bool m_Canceled;
        TaskHandlerType m_TaskHandler;

    public:

        /**
         * @brief Creates the instance for the task when exit the scope.
         * @param EventHandler The instance of the task handler.
        */
        explicit ScopeExitTaskHandler(TaskHandlerType&& EventHandler) :
            m_Canceled(false),
            m_TaskHandler(std::forward<TaskHandlerType>(EventHandler))
        {

        }

        /**
         * @brief Executes and uninitializes the instance for the task when
         *        exit the scope.
        */
        ~ScopeExitTaskHandler()
        {
            if (!this->m_Canceled)
            {
                this->m_TaskHandler();
            }
        }

        /**
         * @brief Cancels the task when exit the scope.
        */
        void Cancel()
        {
            this->m_Canceled = true;
        }
    };

    /**
     * @brief Converts from the onebyte or multibyte string to the Int32 integer.
     * @param Source The onebyte or multibyte string.
     * @param Radix The number base to use.
     * @return The converted Int32 integer value.
     * @remark For more information, see strtol.
     */
    std::int32_t ToInt32(
        std::string const& Source,
        std::uint8_t const& Radix = 10);

    /**
     * @brief Converts from the onebyte or multibyte string to the Int64 integer.
     * @param Source The onebyte or multibyte string.
     * @param Radix The number base to use.
     * @return The converted Int64 integer value.
     * @remark For more information, see strtoll.
     */
    std::int64_t ToInt64(
        std::string const& Source,
        std::uint8_t const& Radix = 10);

    /**
     * @brief Converts from the onebyte or multibyte string to the UInt32 integer.
     * @param Source The onebyte or multibyte string.
     * @param Radix The number base to use.
     * @return The converted UInt32 integer value.
     * @remark For more information, see strtoul.
     */
    std::uint32_t ToUInt32(
        std::string const& Source,
        std::uint8_t const& Radix = 10);

    /**
     * @brief Converts from the onebyte or multibyte string to the UInt64 integer.
     * @param Source The onebyte or multibyte string.
     * @param Radix The number base to use.
     * @return The converted UInt64 integer value.
     * @remark For more information, see strtoull.
     */
    std::uint64_t ToUInt64(
        std::string const& Source,
        std::uint8_t const& Radix = 10);
}

#endif // !MILE_POSIX_HELPERS_CPPBASE
