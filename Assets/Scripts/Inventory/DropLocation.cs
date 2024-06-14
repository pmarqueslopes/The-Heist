using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropLocation : MonoBehaviour
{
  void OnTriggerEnter(Collider other)
  {
    if (other.CompareTag("Item"))
    {
        other.TryGetComponent(out Item item);
        TotalMoney.instance.AddMoneyRpc(item.itemValue);
        other.GetComponent<PickupObject>().enabled = false;

    }
  }

  
  
}


