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
    public KMBossModule BossManager;
    public KMBombModule Module;
    public KMBombInfo Bomb;
    public PressureModuleService Service;

    public KMSelectable Button;
    public GameObject ButtonTop;
    public AudioClip steamAudioClip;
    public AudioClip warningAudioClip;
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

    private static bool bossModule = true;
    private bool thisIsBossModule;

    private bool isActivated = false;
    private bool ZenModeActive;
    private bool steamPlaying = false;
    private bool buttonPressed = false;
    private float buttonSinglePressTimer = 0;
    private Vector3 buttonOriginalPos;

    private float howLongToGlitch = 0.7f;
    private float howLongUntilGlitch;
    private float meterGlitchingTimer = 0;
    private float meterToGlitchTimer = 0;

    private KMAudio.KMAudioRef leakSfxRef;
    private float leakSfxTimeToReplay;

    void Awake()
    {
        bossModule = true;
    }
    void Start()
    {
        thisIsBossModule = bossModule;
        if (bossModule) bossModule = false;
        PressureMeterText.text = "0%";

        buttonOriginalPos = ButtonTop.transform.localPosition;
        /*
        // Find the Pressure Service and obtain the list of module authors/release dates
        Service = FindObjectOfType<PressureModuleService>();
        if (Service == null)
        {
            Debug.LogFormat(@"[Pressure #{0}] Catastrophic problem: Pressure Service is not present.");
            Module.HandlePass();
        }
        */
        PressureToDeplete = CalculatePressureToDeplete();

        Module.OnActivate += OnActivation;
        Module.OnPass += OnPassed;
    }

    /// <summary>
    /// Calculates the dynamic amount to add to the pressure per second
    /// </summary>
    /// <returns>How much pressure to add per second</returns>
    private float CalculatePressureToDeplete()
    {
        // For this calculation, we set it to fill up and explode at 1/3 of the bomb's total time
        float bombTime = Bomb.GetTime();
        float pressure = 100 / (bombTime / 3);
        return pressure;
    }

    private void OnActivation()
    {
        isActivated = true;

        gameObject.AddComponent<KMAudio>().PlaySoundAtTransform(warningAudioClip.name, transform.parent);

        if(ZenModeActive)
        {
            PressureToDeplete = 0;
            PressureMeterText.text = "Zen";
        }
        if (!thisIsBossModule)
        {
            PressureToDeplete = 0;
            PressureMeterText.text = "Dupe";
        }

        Button.OnInteract += ButtonPress;
        Button.OnInteractEnded += ButtonRelease;

        CurrentPressure = 0;
        howLongUntilGlitch = Random.Range(MeterGlitchRange.x, MeterGlitchRange.y);
    }

    private bool OnPassed()
    {
        isActivated = false;
        PressureMeterText.text = ":D";
        bossModule = false;
        return false;
    }

    private bool ButtonPress()
    {
        leakSfxRef.StopSound();
        steamPlaying = false;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        Button.GetComponent<KMSelectable>().AddInteractionPunch();
        buttonPressed = true;
        buttonSinglePressTimer = 0;
        Button.gameObject.transform.Translate(
            Vector3.Scale(ButtonPushedOffset, transform.parent.parent.localScale));
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
        // If the module contains the word The 
        List<string> theModules = Bomb.GetSolvableModuleNames()
            .FindAll(module => module.ToLower().Contains(" the ") 
                               || module.ToLower().StartsWith("the "));
        // If the module starts with a vowel
        List<string> vowelModules = Bomb.GetSolvableModuleNames()
            .FindAll(module => "aeiou".IndexOf(module.ToLower()[0]) >= 0);
        // If the module contains "P" or "R"
        List<string> prContainsModules = Bomb.GetSolvableModuleNames()
            .FindAll(module => module.ContainsIgnoreCase("p") || module.ContainsIgnoreCase("r"));
        theModules = RemoveIgnoredModules(theModules);
        vowelModules = RemoveIgnoredModules(vowelModules);
        prContainsModules = RemoveIgnoredModules(prContainsModules);
        foreach (string module in Bomb.GetSolvedModuleNames())
        {
            theModules.Remove(module);
            vowelModules.Remove(module);
            prContainsModules.Remove(module);
        }

        bool theModulePassed = theModules.Count <= 0;
        bool singleAuthorsPassed = vowelModules.Count <= 0;
        bool beforeModulePassed = prContainsModules.Count <= 0;

        if (theModulePassed && singleAuthorsPassed && beforeModulePassed)
        {
            Module.HandlePass();
        }
        else
        {
            Module.HandleStrike();
        }
    }

    private List<string> RemoveIgnoredModules(List<string> list)
    {
        string[] ignored = BossManager.GetIgnoredModules(Module, DefaultIgnoreList);

        list.RemoveAll(module => ignored.Contains(module));
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
        if (ZenModeActive) return;
        if (!thisIsBossModule) return;

        if (!buttonPressed)
        {
            if (!steamPlaying)
            {
                leakSfxRef = Audio.PlaySoundAtTransformWithRef(steamAudioClip.name, transform);
                steamPlaying = true;
                leakSfxTimeToReplay = Time.fixedTime + steamAudioClip.length;
            }
            else
            {
                if (Time.fixedTime > leakSfxTimeToReplay)
                {
                    steamPlaying = false;
                }
            }
            CurrentPressure += PressureToDeplete * Time.deltaTime;
        }
        if (CurrentPressure >= 100)
        {
            Module.HandleStrike();
        }

        UpdatePressureMeter();
    }

    private string[] DefaultIgnoreList = new[]
    {
        "Pressure",
        "14",
        "Bamboozling Time Keeper",
        "Brainf---",
        "Forget Enigma",
        "Forget Everything",
        "Forget It Not",
        "Forget Me Later",
        "Forget Me Not",
        "Forget Perspective",
        "Forget The Colors",
        "Forget Them All",
        "Forget This",
        "Forget Us Not",
        "Iconic",
        "Organization",
        "Purgatory",
        "RPS Judging",
        "Simon Forgets",
        "Simon's Stages",
        "Souvenir",
        "Tallordered Keys",
        "The Time Keeper",
        "The Troll",
        "The Twin",
        "The Very Annoying Button",
        "Timing is Everything",
        "Turn The Key",
        "Ultimate Custom Night",
        "Übermodule"
    };
}
