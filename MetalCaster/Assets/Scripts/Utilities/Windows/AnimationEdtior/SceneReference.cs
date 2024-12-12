using UnityEngine;

[System.Serializable]
public class SceneReference
{
    [SerializeField] public string objectName;
    [SerializeField] public GameObject obj;

    public SceneReference(GameObject obj) => Set(obj);

    public void Fix()
    {
        Debug.Log("Fixed");
        objectName = obj.name;
    }

    public void Set(GameObject obj)
    {
        objectName = obj.name;
        this.obj = obj;
    }

    public GameObject Get()
    {
        if (obj == null && !string.IsNullOrEmpty(objectName)) obj = GameObject.Find(objectName);

        return obj;
    }
}
