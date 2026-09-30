using UnityEngine;
using System.Collections;
namespace AdaptiveOhmsLaw
{
    public class WattsonController : MonoBehaviour
    {
        [SerializeField] Transform visual, lookTarget, dialogueAnchor;
        [SerializeField] Animator animator;
        [SerializeField] AudioSource voice;
        Transform learner;
        DialoguePresenter dialogue;
        Quaternion targetRotation;
        Vector3 initialPosition;
        Vector3 homeWorldPosition, movementTarget;
        Quaternion homeWorldRotation;
        [SerializeField, Range(.2f, 4f)] float movementSpeed = 1.5f;
        [SerializeField] Transform pointingArm;
        Quaternion armRestRotation;
        bool pointing;
        bool hasMovementTarget;
        bool talking;
        bool conversationTalking;
        Coroutine chatReaction;
        WattsonSpeech speech;
        public WattsonSpeech Speech => speech;
        void Awake(){speech=GetComponent<WattsonSpeech>();if(speech==null)speech=gameObject.AddComponent<WattsonSpeech>();}
        public void ReactToChat(string text)
        {
            StopChatReaction();FaceLearner();speech.Speak(text);LessonCinematicDirector.Active?.PresentSpokenText(text);chatReaction=StartCoroutine(ChatReaction(Mathf.Clamp(text.Length*.035f,2,12)));
        }
        IEnumerator ChatReaction(float seconds){conversationTalking=true;yield return new WaitForSecondsRealtime(seconds);while(speech.IsSpeaking)yield return null;conversationTalking=false;chatReaction=null;LessonCinematicDirector.Active?.EndShot();}
        public void StopChatReaction(){if(chatReaction!=null)StopCoroutine(chatReaction);chatReaction=null;conversationTalking=false;speech?.Stop();}
        public void StopVoice(){StopChatReaction();dialogue?.PauseVoice(true);}
        public void ResumeLessonVoice(){dialogue?.PauseVoice(false);}
        public void Configure(Transform mesh, Transform target, Transform anchor, Animator animation, AudioSource audio)
        { visual = mesh; lookTarget = target; dialogueAnchor = anchor; animator = animation; voice = audio; }
        public void Bind(Transform cameraTransform, DialoguePresenter presenter)
        {
            if (dialogue != null) { dialogue.LineStarted -= OnLine; dialogue.LineFinished -= OnEnd; }
            learner = cameraTransform; dialogue = presenter; initialPosition = visual != null ? visual.localPosition : Vector3.zero;targetRotation=transform.rotation;
            homeWorldPosition = transform.position; homeWorldRotation = transform.rotation;
            dialogue.BindSpeech(speech);
            if (pointingArm == null)
                foreach (var child in GetComponentsInChildren<Transform>(true))
                    if (child.name == "ArmRight" && child.gameObject.activeInHierarchy) { pointingArm = child; break; }
            if (pointingArm != null) armRestRotation = pointingArm.localRotation;
            dialogue.LineStarted += OnLine; dialogue.LineFinished += OnEnd;
        }
        void OnLine(DialogueLine line) { StopChatReaction();talking = true; FaceLearner(); LessonCinematicDirector.Active?.PresentLine(line); }
        void OnEnd(string nextEvent) { talking = false; LessonCinematicDirector.Active?.EndShot(); }
        public void FaceLearner()
        {
            if (learner == null) return;
            if (lookTarget != null) lookTarget.position = learner.position;
            targetRotation = Quaternion.LookRotation(learner.position - transform.position, Vector3.up);
        }
        public void Speak(DialogueLine line) => dialogue.Show(line);
        public void MoveBeside(Transform subject, float side = 1f)
        {
            if (subject == null || learner == null) return;
            Vector3 towardLearner = learner.position - subject.position; towardLearner.y = 0;
            if (towardLearner.sqrMagnitude < .01f) towardLearner = Vector3.back;
            Vector3 right = Vector3.Cross(towardLearner.normalized, Vector3.up);
            movementTarget = subject.position + right * side * 2f + Vector3.up * 1.1f;
            hasMovementTarget = true;
            pointing = true;
        }
        public void ReturnToHome() { movementTarget = homeWorldPosition; hasMovementTarget = true; pointing = false; }
        // Responsive evidence layouts reposition the companion. Their new pose
        // must also become the return destination after narration or chat.
        public void RebasePresentationHome()
        {
            homeWorldPosition = transform.position; homeWorldRotation = transform.rotation;
            movementTarget = homeWorldPosition; hasMovementTarget = false; pointing = false;
            FaceLearner();
        }
        public bool IsAtMovementTarget => !hasMovementTarget || Vector3.Distance(transform.position, movementTarget) < .04f;
        bool presentationPaused;
        public void SetPresentationPaused(bool value) => presentationPaused = value;
        void Update()
        {
            if (learner == null || presentationPaused) return;
            if (hasMovementTarget)
            {
                transform.position = Vector3.MoveTowards(transform.position, movementTarget, movementSpeed * Time.unscaledDeltaTime);
                if (Vector3.Distance(transform.position, movementTarget) < .02f) hasMovementTarget = false;
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2.5f);
            if (pointingArm != null)
                pointingArm.localRotation = Quaternion.Slerp(pointingArm.localRotation,
                    armRestRotation * Quaternion.Euler(pointing ? -35f : 0, 0, pointing ? -45f : 0), Time.deltaTime * 4f);
            if (visual != null)
            {
                visual.localPosition = initialPosition + Vector3.up * (Mathf.Sin(Time.time * (talking||conversationTalking ? 3 : 1.4f)) * .05f);
                visual.localRotation = Quaternion.Euler(0, 0, talking ? Mathf.Sin(Time.time * 4f) * 2.5f : 0);
            }
        }
        void OnDestroy() { if (dialogue != null) { dialogue.LineStarted -= OnLine; dialogue.LineFinished -= OnEnd; } }
    }
}
