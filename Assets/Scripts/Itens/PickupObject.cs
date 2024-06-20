using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Outline))]
public class PickupObject : NetworkBehaviour, Interactable
{
    public Item item;

    private Rigidbody m_Rigidbody;
    private BoxCollider m_Collider;

    private NetworkVariable<bool> m_IsGrabbed = new NetworkVariable<bool>();

    [SerializeField]
    private Outline outline;

    private MeshRenderer _renderer;

    public bool alreadyCollected;

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Collider = GetComponent<BoxCollider>();
        _renderer = GetComponent<MeshRenderer>();
        outline = GetComponent<Outline>();
        outline.enabled = false;
    }
    public IEnumerator Start()
    {
        yield return new WaitUntil(() => PlayerActions.Instance != null);
        yield return new WaitUntil(() => PlayerActions.Instance.ready);
        PlayerActions.Instance.PlayerInputActions.Player.Release.performed += DropItem;
        PlayerActions.Instance.PlayerInputActions.Player.DropRelic.performed += DropRelic;
    }

    private void OnDisable()
    {
        PlayerActions.Instance.PlayerInputActions.Player.Release.performed -= DropItem;
        PlayerActions.Instance.PlayerInputActions.Player.DropRelic.performed -= DropRelic;
    }

    private void DropRelic(InputAction.CallbackContext obj)
    {
        if (m_IsGrabbed.Value && IsOwner)
        {
            if (item.isRelic)
            {
                DropRelicServerRpc();
            }
        }
    }

    private void DropItem(InputAction.CallbackContext obj)
    {
        if (IsOwner && !item.isRelic && m_IsGrabbed.Value)
        {
            Debug.Log($"Called {++calledTimes} times");
            if (Inventory.Instance.items[ItemSelect.Instance.currentItemIndex] == item)
                ReleaseServerRpc(ItemSelect.Instance.currentItemIndex);
        }
    }

    private static int calledTimes = 0;

    public string getDisplayText()
    {
        return "Pick \"E\"";

    }

    public void Interact()
    {
        TryGrabServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryGrabServerRpc(ServerRpcParams serverRpcParams = default)
    {
        if (m_IsGrabbed.Value) return;
        ulong senderClientId = serverRpcParams.Receive.SenderClientId;
        NetworkObject senderPlayerObject = PlayersManager.Players[senderClientId].NetworkObject;
        if (senderPlayerObject == null) return;
        NetworkObject.ChangeOwnership(senderClientId);
        if (!item.isRelic && Inventory.Instance.hasEmptySlot())
        {
            TryGrabItemOwnerRpc();
        }
        else if (Inventory.Instance.bagWeight + item.itemWeight <= Inventory.MaxWeight)
        {
            TryGrabRelicOwnerRpc();
        }
        else
        {
            return;
        }

        ParentObjectRpc(senderClientId);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void ParentObjectRpc(ulong senderClientId)
    {
        NetworkObject senderPlayerObject = PlayersManager.Players[senderClientId].NetworkObject;
        Transform playerTransform = senderPlayerObject.GetComponent<PlayerActions>().drop;
        transform.parent = senderPlayerObject.transform;
        transform.localPosition = playerTransform.localPosition;
        m_IsGrabbed.Value = true;
        m_Collider.enabled = false;
        SomeRpc(false);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    private void SomeRpc(bool _enabled)
    {
        _renderer.enabled = _enabled;
        m_Rigidbody.isKinematic = !_enabled;
        m_Collider.isTrigger = !_enabled;
    }

    [Rpc(SendTo.Owner)]
    private void TryGrabItemOwnerRpc()
    {
        Inventory.Instance.AddItem(item);
    }

    [Rpc(SendTo.Owner)]
    private void TryGrabRelicOwnerRpc()
    {
        Inventory.Instance.AddRelic(item);
    }

    [ServerRpc]
    private void ReleaseServerRpc(int index)
    {
        ReleaseItemOwnerRpc(index);
        NetworkObject.RemoveOwnership();
        UnparentObjectRpc();
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void UnparentObjectRpc()
    {
        transform.parent = null;
        m_IsGrabbed.Value = false;
        _renderer.enabled = true;
        m_Collider.enabled = true;
        SomeRpc(true);
        m_Rigidbody.AddForce(transform.forward * 0.5f, ForceMode.Impulse);
    }

    [Rpc(SendTo.Owner)]
    public void ReleaseItemOwnerRpc(int index)
    {
        Inventory.Instance.RemoveItem(index);
    }

    [ServerRpc]
    void DropRelicServerRpc()
    {
        ReleaseRelicOwnerRpc();
        NetworkObject.RemoveOwnership();
        UnparentObjectRpc();
    }
    [Rpc(SendTo.Owner)]
    public void ReleaseRelicOwnerRpc()
    {
        Inventory.Instance.RemoveRelic();
    }
}