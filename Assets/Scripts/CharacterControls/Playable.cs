using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Playable : NetworkBehaviour
{
    public NetworkVariable<NetworkBehaviourReference> controlPlayerNetworkBehaviourReference = new NetworkVariable<NetworkBehaviourReference>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
}
