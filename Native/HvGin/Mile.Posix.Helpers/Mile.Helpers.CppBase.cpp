/*
 * PROJECT:   Mouri Internal Library Essentials
 * FILE:      Mile.Helpers.CppBase.cpp
 * PURPOSE:   Implementation for the essential POSIX helper functions
 *
 * LICENSE:   The MIT License
 *
 * MAINTAINER: MouriNaruto (Kenji.Mouri@outlook.com)
 */

#include "Mile.Helpers.CppBase.h"

#include <cstdarg>

std::string Mile::VFormatString(
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

std::string Mile::FormatString(
    _In_ char const* const Format,
    ...)
{
    va_list ArgList;
    va_start(ArgList, Format);
    std::string Result = Mile::VFormatString(Format, ArgList);
    va_end(ArgList);
    return Result;
}

std::int32_t Mile::ToInt32(
    std::string const& Source,
    std::uint8_t const& Radix)
{
    return static_cast<std::int32_t>(
        std::strtol(Source.c_str(), nullptr, Radix));
}

std::int64_t Mile::ToInt64(
    std::string const& Source,
    std::uint8_t const& Radix)
{
    return std::strtoll(Source.c_str(), nullptr, Radix);
}

std::uint32_t Mile::ToUInt32(
    std::string const& Source,
    std::uint8_t const& Radix)
{
    return static_cast<std::uint32_t>(
        std::strtoul(Source.c_str(), nullptr, Radix));
}

std::uint64_t Mile::ToUInt64(
    std::string const& Source,
    std::uint8_t const& Radix)
{
    return std::strtoull(Source.c_str(), nullptr, Radix);
}
