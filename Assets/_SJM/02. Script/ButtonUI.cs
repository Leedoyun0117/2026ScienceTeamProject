using UnityEngine;
using DG.Tweening;
public class ButtonUI : MonoBehaviour
{
   [SerializeField] private Transform _button;
   [SerializeField] private Transform _button2;
   private Sequence _sequence;
   [SerializeField] bool _isClick = false;
   public void UIDown()
   {
      if (_isClick == true)
      {
         _sequence.Append(_button.transform.DOLocalMoveY(415, 0.5f));
         _sequence.Join(_button2.transform.DOLocalMoveY(415, 0.5f));
         _isClick = false;
      }
      else if (_isClick == false)
      {
         _sequence = DOTween.Sequence();
         _sequence.Append(_button.transform.DOLocalMoveY(-700, 0.5f));
         _sequence.Join(_button2.transform.DOLocalMoveY(-700, 0.5f));
         _isClick = true;
      }
    
     
   }



}
