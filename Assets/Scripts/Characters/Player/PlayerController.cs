using UnityEngine;
using Game.Combat;
using Game.Characters;
using Game.Items.Equipment;
using Game.QuickSlot;
using Game.Weapons;
using Game.SceneFlow;

namespace Game.Characters.Player
{
    /// <summary>
    /// Thin composition root for the playable character: receives input and
    /// forwards it to CharacterMotor/WeaponLoadout, nothing more. Inventory,
    /// quickslots and weapon handling keep their own input handlers (see
    /// character-system.md) — this class only wires the references together.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private HealthComponent health;
        [SerializeField] private CharacterMotor motor;
        [SerializeField] private FactionMember faction;
        [SerializeField] private StaminaController stamina;
        [SerializeField] private AttributeSet attributes;
        [SerializeField] private ContainerEquipmentController equipment;
        [SerializeField] private QuickSlotController quickSlots;
        [SerializeField] private WeaponLoadout weaponLoadout;
        [SerializeField, Min(0f)] private float moveLookAheadDistance = 1f;

        public HealthComponent Health => health;
        public CharacterMotor Motor => motor;
        public FactionMember Faction => faction;
        public StaminaController Stamina => stamina;
        public AttributeSet Attributes => attributes;
        public ContainerEquipmentController Equipment => equipment;
        public QuickSlotController QuickSlots => quickSlots;
        public WeaponLoadout WeaponLoadout => weaponLoadout;

        private void Awake()
        {
            faction.SetFaction(Game.Characters.Faction.Player);

            // Null when this scene is entered directly without a boot-scene
            // PlayerRuntimeContext (e.g. isolated testing) - real play always
            // goes through Boot first, so this guard only affects that case.
            if (PlayerRuntimeContext.Instance != null)
            {
                PlayerRuntimeContext.Instance.BindActivePlayer(gameObject);
            }
        }

        public void OnMoveInput(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0001f)
            {
                motor.Stop();
                return;
            }

            Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;
            motor.MoveTo(transform.position + direction * moveLookAheadDistance);
        }

        public void OnAttackInput()
        {
            var context = new WeaponUseContext(gameObject, transform.position, transform.forward);
            weaponLoadout.TryUseActive(context);
        }
    }
}
