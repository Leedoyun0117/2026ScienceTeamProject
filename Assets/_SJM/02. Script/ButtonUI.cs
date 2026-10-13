using UnityEngine;
using DG.Tweening;
public class ButtonUI : MonoBehaviour
{
   [SerializeField] private RectTransform _button;
   [SerializeField] private Transform _startPos;
   [SerializeField] private Transform _endPos;
   private Sequence _sequence;
   [SerializeField] bool _isClick = false;
   public void UIDown()
   {
      if (_isClick == true)
      {
         _sequence.Append(_button.transform.DOLocalMoveY(_endPos.transform.position.y, 0.5f));
         _isClick = false;
      }
      else if (_isClick == false)
      {
         _sequence = DOTween.Sequence();
         _sequence.Append(_button.transform.DOLocalMoveY(_startPos.transform.position.y, 0.5f));
         _isClick = true;
      }
    
   }

}
