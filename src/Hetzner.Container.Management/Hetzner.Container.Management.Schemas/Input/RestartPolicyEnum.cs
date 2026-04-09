using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Hetzner.Container.Management.Schemas.Input;

public enum RestartPolicyEnum
{
    [Display(Name = "no")]
    [EnumMember(Value = "no")]
    No = 1,
    [Display(Name = "always")]
    [EnumMember(Value = "always")]
    Always,
    [Display(Name = "on-failure")]
    [EnumMember(Value = "onFailure")]
    OnFailure,
    [Display(Name = "unless-stopped")]
    [EnumMember(Value = "unlessStopped")]
    UnlessStopped,
}