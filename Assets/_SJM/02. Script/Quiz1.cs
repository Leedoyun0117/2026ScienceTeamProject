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
           _sequence.AppendInterval(2f);
           
           _quiznum++;
       }
       if (_quiznum == 2)
       {
           _sequence = DOTween.Sequence();
           _sequence.Append(_incorrectText.DOFade(1,0.6f));
           _sequence.AppendInterval(2f);
           _sequence.AppendCallback((() => _quizText.text = "dd"));
           _quiznum++;
       }
   }

   public void Incorrect()
   {
       if (_quiznum == 1)
       {
           _sequence = DOTween.Sequence();
           _sequence.Append(_incorrectText.DOFade(1,0.6f));
           _sequence.AppendInterval(2f);
     
           _quiznum++;
       } 
       if (_quiznum == 2)
       {
           _sequence = DOTween.Sequence();
           _sequence.Append(_incorrectText.DOFade(1,0.6f));
           _sequence.AppendInterval(2f);
           
           _quiznum++;
       }
   }
}
