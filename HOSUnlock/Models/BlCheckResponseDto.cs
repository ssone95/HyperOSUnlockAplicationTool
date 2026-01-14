using HOSUnlock.Enums;
using System.Text.Json.Serialization;

namespace HOSUnlock.Models;

public sealed class BlCheckResponseDto
{
    [JsonPropertyName("is_pass")]
    public int IsPass { get; init; }

    [JsonPropertyName("button_state")]
    public int ButtonState { get; init; }

    [JsonPropertyName("deadline_format")]
    public string? DeadlineFormat { get; init; }

    [JsonIgnore]
    public MiEnums.MiIsPassState MiEnumIsPass
    {
        get
        {
            if (Enum.IsDefined(typeof(MiEnums.MiIsPassState), IsPass))
                return (MiEnums.MiIsPassState)IsPass;

            return MiEnums.MiIsPassState.Unknown;
        }
    }

    [JsonIgnore]
    public MiEnums.MiButtonState MiEnumButtonState
    {
        get
        {
            if (Enum.IsDefined(typeof(MiEnums.MiButtonState), ButtonState))
                return (MiEnums.MiButtonState)ButtonState;

            return MiEnums.MiButtonState.Unknown;
        }
    }
}

public sealed class ApplyBlAuthResponseDto
{
    [JsonPropertyName("apply_result")]
    public int? ApplyResult { get; init; }

    [JsonPropertyName("deadline_format")]
    public string? DeadlineFormat { get; init; }

    [JsonIgnore]
    public MiEnums.MiApplyResult ApplyResultState
    {
        get
        {
            if (ApplyResult is null)
                return MiEnums.MiApplyResult.Unknown;

            if (Enum.IsDefined(typeof(MiEnums.MiApplyResult), ApplyResult.Value))
                return (MiEnums.MiApplyResult)ApplyResult.Value;

            return MiEnums.MiApplyResult.Unknown;
        }
    }
}