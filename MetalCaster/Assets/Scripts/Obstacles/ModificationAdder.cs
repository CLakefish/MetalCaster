using UnityEngine;

public class ModificationAdder : MonoBehaviour
{
    [SerializeField] private Modification modification;

    private void OnTriggerStay(Collider other)
    {
        GameDataManager.Instance.ActiveSave.UnlockModification(modification);
    }
}
