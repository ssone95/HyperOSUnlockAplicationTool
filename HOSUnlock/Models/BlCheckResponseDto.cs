using HOSUnlock.Enums;
using System.Text.Json.Serialization;

namespace HOSUnlock.Models
{
    public class BlCheckResponseDto
    {
        [JsonPropertyName("is_pass")]
        public int IsPass { get; set; }

        [JsonPropertyName("button_state")]
        public int ButtonState { get; set; }

        [JsonPropertyName("deadline_format")]
        public string DeadlineFormat { get; set; }

        [JsonIgnore]
        public MiEnums.MiIsPassState MiEnumIsPass
        {
            get
            {
                MiEnums.MiIsPassState state;
                try
                {
                    state = (MiEnums.MiIsPassState)IsPass;
                }
                catch
                {
                    state = MiEnums.MiIsPassState.Unknown;
                }
                return state;
            }
        }

        [JsonIgnore]
        public MiEnums.MiButtonState MiEnumButtonState
        {
            get
            {
                MiEnums.MiButtonState state;
                try
                {
                    state = (MiEnums.MiButtonState)ButtonState;
                }
                catch
                {
                    state = MiEnums.MiButtonState.Unknown;
                }
                return state;
            }
        }
    }

    public class ApplyBlAuthResponseDto
    {
        [JsonPropertyName("apply_result")]
        public int? ApplyResult { get; set; }

        [JsonIgnore]
        public MiEnums.MiApplyResult ApplyResultState
        {
            get
            {
                if (ApplyResult == null)
                {
                    return MiEnums.MiApplyResult.Unknown;
                }

                MiEnums.MiApplyResult result;
                try
                {
                    result = (MiEnums.MiApplyResult)ApplyResult;
                }
                catch
                {
                    result = MiEnums.MiApplyResult.Unknown;
                }
                return result;
            }
        }

        [JsonPropertyName("deadline_format")]
        public string? DeadlineFormat { get; set; }
    }
}