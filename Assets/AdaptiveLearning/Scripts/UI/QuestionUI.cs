using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace AdaptiveOhmsLaw
{
    public class QuestionUI : MonoBehaviour
    {
        [SerializeField] Text prompt;
        [SerializeField] Button[] answers;
        [SerializeField] GameObject answerGroup, decisionGroup;
        [SerializeField] Button retry, keep;
        public event Action<int> Selected;
        public event Action RetryRequested, KeepRequested;
        public void Configure(Text label, Button[] choices, GameObject options, GameObject decisions, Button again, Button retain)
        { prompt = label; answers = choices; answerGroup = options; decisionGroup = decisions; retry = again; keep = retain; }
        void Awake()
        {
            for (int i = 0; i < answers.Length; i++) { int index = i; answers[i].onClick.AddListener(() => Selected?.Invoke(index)); }
            retry.onClick.AddListener(() => RetryRequested?.Invoke()); keep.onClick.AddListener(() => KeepRequested?.Invoke());
        }
        public void Show(QuestionDefinition definition)
        {
            gameObject.SetActive(true); prompt.text = definition.questionText;
            for (int i = 0; i < answers.Length; i++)
            {
                answers[i].gameObject.SetActive(i < definition.answers.Length);
                if (i < definition.answers.Length) answers[i].GetComponentInChildren<Text>().text = ((char)('A' + i)) + "   " + definition.answers[i];
            }
            ShowAnswers();
        }
        public void ShowAnswers()
        {
            answerGroup.SetActive(true); decisionGroup.SetActive(false); SetInteractable(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(answers[0].gameObject);
        }
        public void SetInteractable(bool value) { foreach (var answer in answers) answer.interactable = value; }
        public void ShowDecision()
        {
            answerGroup.SetActive(false); decisionGroup.SetActive(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(retry.gameObject);
        }
    }
}
