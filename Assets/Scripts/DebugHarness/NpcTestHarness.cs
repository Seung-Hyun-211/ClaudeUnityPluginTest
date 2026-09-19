using UnityEngine;
using Game.Characters.Npc;
using Game.Combat;
using Game.Dialogue;
using Game.Interaction;

namespace Game.DebugHarness
{
    /// <summary>
    /// Manual test harness for npc-roles.md. Village NPC needs no AI wiring at
    /// all (proves "AI is optional" per NpcController's own doc comment) - it
    /// only needs to be within InteractionDetector range to show its
    /// DialogueInteractable prompt. Companion NPC gets InitializeAi(CompanionBrain)
    /// called here since IAiBrain instances cannot be assigned in the inspector.
    /// OnGUI only; never shipped in a real build.
    /// </summary>
    public class NpcTestHarness : MonoBehaviour
    {
        [SerializeField] private NpcController villageNpc;
        [SerializeField] private DialogueInteractable villageDialogue;
        [SerializeField] private NpcController companionNpc;
        [SerializeField] private CompanionOrderReceiver companionOrders;
        [SerializeField] private Transform followTarget;
        [SerializeField] private HealthComponent hostileDummy;

        private void Start()
        {
            // Village NPC deliberately never calls InitializeAi - it just stands
            // there with FactionMember(Neutral) + DialogueInteractable, exactly
            // as npc-roles.md's Village composition specifies.
            companionNpc.InitializeAi(new CompanionBrain(followTarget));
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 460, 320), GUI.skin.box);
            GUILayout.Label("NPC Test Harness");
            GUILayout.Label($"Village NPC ({villageDialogue.PromptText}) - has no DialogueSequence assigned in this scene, so it shows no prompt and F does nothing (its CanInteract is false); see Test_Dialogue for a working dialogue.");

            GUILayout.Space(8);
            GUILayout.Label($"Companion order: {companionOrders.CurrentOrder}");
            if (GUILayout.Button("Order: Follow")) companionOrders.SetOrder(CompanionOrder.Follow);
            if (GUILayout.Button("Order: Hold")) companionOrders.SetOrder(CompanionOrder.Hold);
            if (GUILayout.Button("Order: Attack hostile dummy")) companionOrders.SetAttackTarget(hostileDummy);
            if (GUILayout.Button("Damage hostile dummy (30)") && hostileDummy.IsAlive)
            {
                hostileDummy.TakeDamage(new DamageInfo(30f, DamageType.Physical, null, hostileDummy.transform.position));
            }
            GUILayout.Label($"Hostile dummy HP: {hostileDummy.Current}/{hostileDummy.Max} alive={hostileDummy.IsAlive}");
            GUILayout.EndArea();
        }
    }
}
