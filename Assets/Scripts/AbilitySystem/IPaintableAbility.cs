using UnityEngine;
using Unity;
public interface IPaintableAbility
{
    void StartPainting(Vector3 startPos);
    void UpdatePainting(Vector3 currentPos);
    void FinishPainting(Vector3 endPos);
    void CancelPainting();
}