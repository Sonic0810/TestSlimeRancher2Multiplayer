using MelonLoader;
using UnityEngine;

namespace SR2MP.Components.World;

[RegisterTypeInIl2Cpp(false)]
public class NetworkGadget : MonoBehaviour
{
    public string GadgetId { get; set; } = string.Empty;
}
