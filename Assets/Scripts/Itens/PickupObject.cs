using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PickupObject : NetworkBehaviour
{
    public Item item;

    private bool canGrab;
    private Rigidbody m_Rigidbody;
    private BoxCollider m_Collider;

    private NetworkVariable<bool> m_IsGrabbed = new NetworkVariable<bool>();
    private PlayerInputActions controle_player;
    private InputAction grab, release, dropRelic;

    private MeshRenderer _renderer;

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Collider = GetComponent<BoxCollider>();
        controle_player = new PlayerInputActions();
        grab = controle_player.Player.Grab;
        grab.Enable();
        grab.performed += Grab;

        release = controle_player.Player.Release;
        release.Enable();
        release.performed += Release;
        _renderer = GetComponent<MeshRenderer>();

        dropRelic = controle_player.Player.DropRelic;
        dropRelic.Enable();
        dropRelic.performed += DropRelic;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("View"))
        {
            canGrab = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("View"))
        {
            canGrab = false;
        }
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

    private void Release(InputAction.CallbackContext obj)
    {
        if (m_IsGrabbed.Value && IsOwner)
        {
            ReleaseServerRpc();
        }
    }

    private void Grab(InputAction.CallbackContext obj)
    {
        if (canGrab) TryGrabServerRpc();
    }

    private void FixedUpdate()
    {
        if (m_Rigidbody)
        {
            m_Rigidbody.isKinematic = !IsServer || m_IsGrabbed.Value;
        }

        if (m_Collider)
        {
            m_Collider.isTrigger = !IsServer || m_IsGrabbed.Value;
        }
    }

    public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
    {
        if (parentNetworkObject != null && (IsOwner || IsServer))
        {
            transform.localPosition = Vector3.up * 2;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryGrabServerRpc(ServerRpcParams serverRpcParams = default)
    {
        if (m_IsGrabbed.Value) return;
        ulong senderClientId = serverRpcParams.Receive.SenderClientId;
        NetworkObject senderPlayerObject = PlayerPickup.Players[senderClientId].NetworkObject;
        if (senderPlayerObject == null) return;
        NetworkObject.ChangeOwnership(senderClientId);
        if (!item.isRelic && Inventory.Instance.itemCount < Inventory.SLOTS)
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

        transform.parent = senderPlayerObject.transform;
        transform.localPosition = new Vector3(0.473f, 0.605f, -0.314f);
        m_IsGrabbed.Value = true;
        _renderer.enabled = false;
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
    private void ReleaseServerRpc()
    {
        if (!item.isRelic && Inventory.Instance.inventorySlots[ItemSelect.Instance.currentItem] && m_IsGrabbed.Value)
        {
            ReleaseItemOwnerRpc();
            NetworkObject.RemoveOwnership();
            transform.parent = null;
            m_IsGrabbed.Value = false;
            _renderer.enabled = true;
        }
    }

    [ServerRpc]
    void DropRelicServerRpc()
    {
        if (Inventory.Instance.relics.Count > 0)
        {
            ReleaseRelicOwnerRpc();
            NetworkObject.RemoveOwnership();
            transform.parent = null;
            m_IsGrabbed.Value = false;
            _renderer.enabled = true;
        }
    }

    [Rpc(SendTo.Owner)]
    public void ReleaseItemOwnerRpc()
    {
        Inventory.Instance.RemoveItem(ItemSelect.Instance.currentItem);
    }

    [Rpc(SendTo.Owner)]
    public void ReleaseRelicOwnerRpc()
    {
        Inventory.Instance.RemoveRelic();
    }
}