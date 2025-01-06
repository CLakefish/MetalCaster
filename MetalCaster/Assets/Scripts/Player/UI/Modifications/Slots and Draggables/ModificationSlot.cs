using UnityEngine;
using UnityEngine.EventSystems;

public class ModificationSlot : Slot
{
    private ModificationMenu context;
    public  ModificationMenu Menu => context;
    public int Slot { get; private set; }

    public void Initialize(ModificationMenu context, int index)
    {
        this.context = context;
        this.Slot    = index;
    }
}
