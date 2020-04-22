using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonComponent : MonoBehaviour {

	public KMSelectable button;
	public TextMesh PressureMeterText;

	public float CurrentPressure;

	private bool isActivated = false;

	void Start () 
	{
		PressureMeterText.text = "0%";

		GetComponent<KMBombModule>().OnActivate += OnActivation;
	}

	private void OnActivation()
	{
		isActivated = true;

		button.OnInteract += ButtonPress;
		button.OnInteractEnded += ButtonRelease;

		CurrentPressure = 0;
	}

	private bool ButtonPress()
	{
		GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		GetComponent<KMSelectable>().AddInteractionPunch();
		return false;
	}

	private void ButtonRelease()
	{
		GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
		GetComponent<KMSelectable>().AddInteractionPunch();
		return;
	}

	private void UpdatePressureMeter()
	{
		PressureMeterText.text = Mathf.Floor(CurrentPressure) + "%";
	}
	
	void Update () 
	{
		CurrentPressure += 0.2f * Time.deltaTime;
		if(CurrentPressure >= 100)
		{
			GetComponent<KMBombModule>().HandleStrike();
		}

		UpdatePressureMeter();
	}
}
