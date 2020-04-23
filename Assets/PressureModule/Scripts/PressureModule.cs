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

        PressureToDeplete = (1 / Bomb.GetSolvableModuleIDs().Count + 1) / PressureDepletionDivider;

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
            List<string> theModules = Bomb.GetSolvableModuleNames()
                .FindAll(module => module.StartsWith("The "));
            bool theModulePassed = theModules
                .All(module => Bomb.GetSolvedModuleNames().Contains(module));
            List<string> singleAuthorModules = Bomb.GetSolvableModuleIDs()
                .FindAll(module => Service.GetAuthors(module).Length == 1);
            bool singleAuthorsPassed = singleAuthorModules
                .All(module => Bomb.GetSolvedModuleIDs().Contains(module));
            List<string> beforeModules = Bomb.GetSolvableModuleIDs()
                .FindAll(module => Service.GetReleaseDate(module)
                               .CompareTo(DateToCompare) < 0);
            bool beforeModulePassed = beforeModules
                .All(module => Bomb.GetSolvedModuleIDs().Contains(module));

            if (theModulePassed && singleAuthorsPassed && beforeModulePassed)
            {
                Module.HandlePass();
            }
            else
            {
                Module.HandleStrike();
            }
        }
    }

    private void UpdatePressureMeter()
    {
        if (MeterGlitching)
        {
            PressureMeterText.text = "";
            for (int i = 0; i < Random.Range(0, 5); i++)
            {
                PressureMeterText.text += GlitchGlyphs[Random.Range(0, GlitchGlyphs.Length)];
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
