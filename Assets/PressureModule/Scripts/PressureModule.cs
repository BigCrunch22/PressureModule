using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
// ReSharper disable InconsistentNaming

public class PressureModule : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombModule Module;
    public KMBombInfo Bomb;
    public PressureModuleService Service;

    public KMSelectable Button;
    public GameObject ButtonTop;
    public TextMesh PressureMeterText;
    public DateTime DateToCompare = new DateTime(2018, 1, 1);
    public Vector3 ButtonPushedOffset = new Vector3(0, -0.02f, 0);
    public float ButtonDetectSinglePressTime = 0.1f;
    public Vector2 MeterGlitchRange = new Vector2(5, 10);
    public string GlitchGlyphs = "abcdefghijklmnopqrstuvwxyz0123456789-=!@#$%^&*()_+[]\\{}|;':\",.<>/?";

    public float CurrentPressure;
    /// <summary>
    /// Devide the calculated depletion by the number 
    /// </summary>
    public float PressureDepletionDivider = 2;
    public float PressureToDeplete;
    public bool MeterGlitching = false;

    private bool isActivated = false;
    private bool buttonPressed = false;
    private float buttonSinglePressTimer = 0;
    private Vector3 buttonOriginalPos;

    private float howLongToGlitch = 0.7f;
    private float howLongUntilGlitch;
    private float meterGlitchingTimer = 0;
    private float meterToGlitchTimer = 0;

    void Start()
    {
        PressureMeterText.text = "0%";

        buttonOriginalPos = ButtonTop.transform.localPosition;
        // Find the Pressure Service and obtain the list of module authors/release dates
        Service = FindObjectOfType<PressureModuleService>();
        if (Service == null)
        {
            Debug.LogFormat(@"[Pressure #{0}] Catastrophic problem: Pressure Service is not present.");
        }

        if (Bomb.GetSolvableModuleIDs().Count <= 2)
        {
            PressureToDeplete = 2;
        }
        else
        {
            PressureToDeplete = (1 / Bomb.GetSolvableModuleIDs().Count + 1) / PressureDepletionDivider;
        }

        Module.OnActivate += OnActivation;
        Module.OnPass += OnPassed;
    }

    private void OnActivation()
    {
        isActivated = true;

        Button.OnInteract += ButtonPress;
        Button.OnInteractEnded += ButtonRelease;

        CurrentPressure = 0;
        howLongUntilGlitch = Random.Range(MeterGlitchRange.x, MeterGlitchRange.y);
    }

    private bool OnPassed()
    {
        isActivated = false;
        PressureMeterText.text = ":D";
        return false;
    }

    private bool ButtonPress()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        Button.GetComponent<KMSelectable>().AddInteractionPunch();
        buttonPressed = true;
        buttonSinglePressTimer = 0;
        Button.gameObject.transform.Translate(ButtonPushedOffset);
        return false;
    }

    private void ButtonRelease()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
        Button.GetComponent<KMSelectable>().AddInteractionPunch();
        Button.gameObject.transform.localPosition = buttonOriginalPos;
        buttonPressed = false;

        if (buttonSinglePressTimer < ButtonDetectSinglePressTime)
        {
            ButtonSinglePressed();
        }
    }

    private void ButtonSinglePressed()
    {
        // If the module starts with The 
        List<string> theModules = Bomb.GetSolvableModuleNames()
            .FindAll(module => module.StartsWith("The "));
        theModules = RemoveSelfFromList(theModules, false);
        foreach (string module in Bomb.GetSolvedModuleNames())
        {
            theModules.Remove(module);
        }

        bool theModulePassed = theModules.Count <= 0;

        // If the module has 1 Author
        List<string> singleAuthorModules = Bomb.GetSolvableModuleIDs()
            .FindAll(module => Service.GetAuthors(module).Length == 1);
        singleAuthorModules = RemoveSelfFromList(singleAuthorModules);
        // If the module was made before 2018
        List<string> beforeModules = Bomb.GetSolvableModuleIDs()
            .FindAll(module => Service.GetReleaseDate(module)
                .CompareTo(DateToCompare) < 0);
        beforeModules = RemoveSelfFromList(beforeModules);
        foreach (string module in Bomb.GetSolvedModuleIDs())
        {
            singleAuthorModules.Remove(module);
            beforeModules.Remove(module);
        }

        bool singleAuthorsPassed = singleAuthorModules.Count <= 0;
        bool beforeModulePassed = beforeModules.Count <= 0;

        if (theModulePassed && singleAuthorsPassed && beforeModulePassed)
        {
            Module.HandlePass();
        }
        else
        {
            Module.HandleStrike();
        }
    }

    private List<string> RemoveSelfFromList(List<string> list, bool id = true)
    {
        list.RemoveAll(module => module == (id ? Module.ModuleType : Module.ModuleDisplayName));
        return list;
    }

    private void UpdatePressureMeter()
    {
        if (MeterGlitching)
        {
            PressureMeterText.text = "";
            for (int i = 0; i < Random.Range(0, 5); i++)
            {
                string glyph = GlitchGlyphs[Random.Range(0, GlitchGlyphs.Length)].ToString();
                if (Random.Range(0, 1) == 1) glyph = glyph.ToUpper();
                PressureMeterText.text += glyph;
            }
        }
        else
        {
            PressureMeterText.text = Mathf.Floor(CurrentPressure) + "%";
        }
    }

    void Update()
    {
        if (!isActivated) return;

        buttonSinglePressTimer += Time.deltaTime;

        if (meterGlitchingTimer > howLongToGlitch)
        {
            MeterGlitching = false;
            meterGlitchingTimer = 0;
            meterToGlitchTimer = 0;
            howLongUntilGlitch = Random.Range(MeterGlitchRange.x, MeterGlitchRange.y);
        }
        else if (MeterGlitching)
        {
            meterGlitchingTimer += Time.deltaTime;
        }
        if (meterToGlitchTimer > howLongUntilGlitch)
        {
            MeterGlitching = true;
            howLongUntilGlitch = Random.Range(MeterGlitchRange.x, MeterGlitchRange.y);
            meterToGlitchTimer = 0;
            meterGlitchingTimer = 0;
        }
        else if (!MeterGlitching)
        {
            meterToGlitchTimer += Time.deltaTime;
        }

        if (!buttonPressed)
        {
            CurrentPressure += PressureToDeplete * Time.deltaTime;
        }
        if (CurrentPressure >= 100)
        {
            Module.HandleStrike();
        }

        UpdatePressureMeter();
    }
}
