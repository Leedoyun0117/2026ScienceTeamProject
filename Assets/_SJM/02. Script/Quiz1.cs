using System;
using TMPro;
using UnityEngine;
using DG.Tweening;
using Unity.VectorGraphics;
using UnityEngine.SceneManagement;

public class Quiz1 : MonoBehaviour
{
   [SerializeField] private TextMeshProUGUI _correctTxet;
   [SerializeField] private TextMeshProUGUI _incorrectText;
   [SerializeField] private GameObject _ui;
   [SerializeField] private TextMeshProUGUI _quizText;
   
   private Sequence _sequence;
   private int _quiznum = 1;
   
 

   public void Correct()
   {
       if (_quiznum == 1)
       {
           _sequence = DOTween.Sequence();
           _sequence.Append(_correctTxet.DOFade(1,0.6f));
           _sequence.Append(_correctTxet.DOFade(0,0.6f));
           _sequence.AppendInterval(0.5f);
           _sequence.AppendCallback((() => _quizText.text = "02. 소비 전력의 정의는 저항에서 1분간 소비되는 전기에너지를 말한다."));
           _quiznum++;
       }
        else if (_quiznum == 2)
       {
           _sequence = DOTween.Sequence();
           _sequence.Append(_correctTxet.DOFade(1,0.6f));
           _sequence.Append(_correctTxet.DOFade(0,0.6f));
           _sequence.AppendInterval(0.5f);
           _sequence.AppendCallback((() => _quizText.text = "03. 2 Ω의 저항에 2 A의 전류가 흐를 때, 저항에서 소비되는 전력은 4 W이다."));
          
           _quiznum++;
       }
        else  if (_quiznum == 3)
        {
            _sequence = DOTween.Sequence();
            _sequence.Append(_correctTxet.DOFade(1,0.6f));
            _sequence.Append(_correctTxet.DOFade(0,0.6f));
            _sequence.AppendInterval(0.5f);
            _sequence.AppendCallback((() => _quizText.text = "퀴즈 끝"));
           
            _quiznum++;
        }
   }

   public void Incorrect()
   {
       if (_quiznum == 1)
       {
           _sequence = DOTween.Sequence();
           _sequence.Append(_incorrectText.DOFade(1,0.6f));
           _sequence.Append(_incorrectText.DOFade(0,0.6f));
           _sequence.AppendInterval(0.5f);
           _sequence.AppendCallback((() => _quizText.text = "02. 소비 전력의 정의는 저항에서 1분간 소비되는 전기에너지를 말한다."));
           _quiznum++;
       } 
      else  if (_quiznum == 2)
       {
           _sequence = DOTween.Sequence();
           _sequence.Append(_incorrectText.DOFade(1,0.6f));
           _sequence.Append(_incorrectText.DOFade(0,0.6f));
           _sequence.AppendInterval(0.5f);
           _sequence.AppendCallback((() => _quizText.text = "03. 2 Ω의 저항에 2 A의 전류가 흐를 때, 저항에서 소비되는 전력은 4 W이다."));
           
           _quiznum++;
       }
      else  if (_quiznum == 3)
      {
          _sequence = DOTween.Sequence();
          _sequence.Append(_incorrectText.DOFade(1,0.6f));
          _sequence.Append(_incorrectText.DOFade(0,0.6f));
          _sequence.AppendInterval(0.5f);
          _sequence.AppendCallback((() => _quizText.text = "퀴즈 끝"));
           
          _quiznum++;
      }
   }
}
