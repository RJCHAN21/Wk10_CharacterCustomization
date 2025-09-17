using System;
using System.Collections.Generic;
using UnityEngine;

public enum PartType
{
    Hair,
    FaceHair,
    EyeColor,
    Skin,
    Cloth,
    Pants
}

[Serializable]
public class PartSlot
{
    public PartType type;

    [Tooltip(
        "Renderer count by type:\n" +
        "• Hair, FaceHair: 1\n" +
        "• EyeColor: 2 (Right/Left eye Back)\n" +
        "• Skin: 6 (Head, Body, R_Arm, L_Arm, R_Foot, L_Foot)\n" +
        "• Cloth: 3 (Body, Right, Left)\n" +
        "• Pants: 2 (Right, Left)"
    )]
    public SpriteRenderer[] renderers;

    [HideInInspector] public int index = 0; // sprite/variant selection index
}

[Serializable]
public class SkinVariant
{
    public Sprite Head;
    public Sprite Body;
    public Sprite RightArm;
    public Sprite LeftArm;
    public Sprite RightFoot;
    public Sprite LeftFoot;
}

[Serializable]
public class ClothVariant
{
    public Sprite Body;
    public Sprite Right;
    public Sprite Left;
}

[Serializable]
public class PantsVariant
{
    public Sprite Right;
    public Sprite Left;
}

[DisallowMultipleComponent]
public class SpriteManager : MonoBehaviour
{
    [Header("Rig Slots")]
    [SerializeField] private List<PartSlot> slots = new();

    [Header("Single Sprite Options")]
    [SerializeField] private Sprite[] HairOptions;
    [SerializeField] private Sprite[] FaceHairOptions;

    [Header("Multi-Sprite Options")]
    [SerializeField] private SkinVariant[]  SkinOptions;
    [SerializeField] private ClothVariant[] ClothOptions;
    [SerializeField] private PantsVariant[] PantsOptions;

    // Runtime Color State
    [Header("Runtime Color State")]
    [Tooltip("If true, the current Hair color will be applied by your runtime code.")]
    public bool HairTintEnabled = true;
    [Tooltip("Chosen by the user via color wheel/hex/HSV/etc.")]
    public Color HairColor = Color.white;

    [Tooltip("If true, the current Eye color will be applied by your runtime code to both eye 'Back' renderers.")]
    public bool EyeTintEnabled = true;
    [Tooltip("Chosen by the user via color wheel/hex/HSV/etc.")]
    public Color EyeColor = Color.white;

    // Optionally add more tintable parts later (e.g., ClothTintEnabled/Color)

    private Dictionary<PartType, PartSlot> _slotByType;
    private Dictionary<PartType, Sprite[]> _spriteOptionsByType; // for Hair/FaceHair only

        // ---------- Lifecycle ----------
    // Build lookups and prepare to accept data from CharacterData.
    private void Awake() { /* build _slotByType, _spriteOptionsByType, etc. */ }

    // Optional: auto-apply from persistent data if present (no logic here)
    private void OnEnable() { /* if (CharacterData.Instance) ApplyAllFromData(CharacterData.Instance); */ }

    // ---------- Public “apply” surface (called by UI or CharacterData) ----------
    // Apply everything in one go (e.g., when a scene loads)
    // public void ApplyAllFromData(CharacterData data) { /* noop stub */ }

    // Apply single part selections (by *index* into your options arrays)
    public void ApplyHairSprite(int index) { /* noop stub */ }
    public void ApplyFaceHairSprite(int index) { /* noop stub */ }
    public void ApplyClothVariant(int index) { /* noop stub */ }  // ClothVariant (Body, R, L)
    public void ApplyPantsVariant(int index) { /* noop stub */ }  // PantsVariant (R, L)
    public void ApplySkinVariant(int index) { /* noop stub */ }   // SkinVariant (Head, Body, R_Arm, L_Arm, R_Foot, L_Foot)

    // Apply *colors* chosen by the user (no presets here)
    public void ApplyHairTint(Color color) { /* noop stub */ }
    public void ApplyEyeTint(Color color) { /* noop stub */ }     // applies to both eye “Back” renderers

    // ---------- Convenience for UI (read-only helpers) ----------
    // Option counts so arrow buttons can clamp/wrap safely without peeking into arrays.
    public int GetOptionCount(PartType type) { return 0; } // stub; Hair/FaceHair from Sprite[], Cloth/Pants/Skin from Variant[]
    public bool SupportsTint(PartType type) { return type == PartType.Hair || type == PartType.EyeColor; }

    // Optional: expose current indices (so UI can show labels) — these mirror your PartSlot.index
    public int GetCurrentIndex(PartType type) { return 0; }    // stub
    public void SetCurrentIndex(PartType type, int index) { /* noop stub */ } // sets PartSlot.index only (no sprite swap)

    // ---------- Optional gender filtering (future) ----------
    // If you add gender filters later, keep index maps here so inspector stays lean.
    // public void SetGender(CharacterData.Gender g) { /* update active index maps */ }
    // public int GetFilteredCount(PartType type) { return 0; } // count through index maps

    // ---------- Events (so UI can listen without tight coupling) ----------
    public event Action<PartType,int> OnPartApplied;  // fired after an Apply* call succeeds
    public event Action<Color>        OnHairTintApplied;
    public event Action<Color>        OnEyeTintApplied;

    // ---------- Internal helpers ----------
    private PartSlot GetSlot(PartType type) { return null; } // stub: lookup in _slotByType
    private void ApplySpriteToRenderers(SpriteRenderer[] renderers, Sprite sprite) { /* noop stub */ }
    private void ApplyPair(SpriteRenderer[] renderers, Sprite right, Sprite left) { /* noop stub */ }
    private void ApplyTriple(SpriteRenderer[] renderers, Sprite body, Sprite right, Sprite left) { /* noop stub */ }
    private void ApplySkinFive(SpriteRenderer[] r, Sprite head, Sprite body, Sprite rArm, Sprite lArm, Sprite rFoot, Sprite lFoot) { /* noop stub */ }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Duplicate slot detection
        var seen = new HashSet<PartType>();
        foreach (var s in slots)
        {
            if (s == null) continue;
            if (!seen.Add(s.type))
                Debug.LogWarning($"[SpriteManager] Duplicate slot for {s.type}. Keep only one.");
        }

        // Renderer count sanity per slot type
        foreach (var s in slots)
        {
            if (s == null) continue;
            int count = s.renderers?.Length ?? 0;
            if (count == 0)
            {
                Debug.LogWarning($"[SpriteManager] {s?.type} has no SpriteRenderers assigned.");
                continue;
            }

            switch (s.type)
            {
                case PartType.Hair:
                case PartType.FaceHair:
                    if (count != 1) Debug.LogWarning($"[SpriteManager] {s.type} should reference ONE renderer. Currently: {count}.");
                    break;

                case PartType.EyeColor:
                    if (count != 2) Debug.LogWarning("[SpriteManager] EyeColor should reference TWO eye 'Back' renderers (Right & Left).");
                    break;

                case PartType.Skin:
                    if (count != 6) Debug.LogWarning("[SpriteManager] Skin should reference SIX renderers (Head, Body, R_Arm, L_Arm, R_Foot, L_Foot).");
                    break;

                case PartType.Cloth:
                    if (count != 3) Debug.LogWarning("[SpriteManager] Cloth should reference THREE renderers (Body, Right, Left).");
                    break;

                case PartType.Pants:
                    if (count != 2) Debug.LogWarning("[SpriteManager] Pants should reference TWO renderers (Right & Left).");
                    break;
            }
        }

        // Presence checks (advisory)
        WarnIfEmptySprites("Hair", HairOptions);
        WarnIfEmptySprites("FaceHair", FaceHairOptions);

        WarnIfEmptyVariants("SkinOptions (Head/Body/R_Arm/L_Arm/R_Foot/L_Foot)", SkinOptions,
            v => v != null && v.Head && v.Body && v.RightArm && v.LeftArm && v.RightFoot && v.LeftFoot);

        WarnIfEmptyVariants("ClothOptions (Body/Right/Left)", ClothOptions,
            v => v != null && v.Body && v.Right && v.Left);

        WarnIfEmptyVariants("PantsOptions (Right/Left)", PantsOptions,
            v => v != null && v.Right && v.Left);
    }

    private void WarnIfEmptySprites(string label, Sprite[] arr)
    {
        if (arr == null || arr.Length == 0)
            Debug.LogWarning($"[SpriteManager] {label} has no sprite options assigned.");
    }

    private void WarnIfEmptyVariants<T>(string label, T[] arr, Func<T, bool> hasAllParts)
    {
        if (arr == null || arr.Length == 0)
        {
            Debug.LogWarning($"[SpriteManager] {label} is empty.");
            return;
        }
        for (int i = 0; i < arr.Length; i++)
        {
            if (!hasAllParts(arr[i]))
                Debug.LogWarning($"[SpriteManager] {label}[{i}] has missing sub-sprites (leaving some null is fine if intentional).");
        }
    }
#endif
}
