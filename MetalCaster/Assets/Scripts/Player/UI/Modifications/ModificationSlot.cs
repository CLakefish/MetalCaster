using UnityEngine;
using UnityEngine.EventSystems;

public class ModificationSlot : Slot
{
    private ModificationMenu context;
    public  ModificationMenu Menu => context;

    public void Initialize(ModificationMenu context) => this.context = context;
}
