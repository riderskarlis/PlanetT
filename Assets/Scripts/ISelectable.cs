using UnityEngine;

public interface ISelectable
{
    string Name { get; }
    string Description { get; }
    void SetSelected(bool value);
    Transform transform { get; }
}
