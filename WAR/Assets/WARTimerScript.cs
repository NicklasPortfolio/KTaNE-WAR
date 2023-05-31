using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;
using System.Reflection;

public class WARTimerScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public TextMesh TimerText;
    public AudioSource Music;

    // Meta
    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    // Module
    private int currentTime = 1;
    private int modulesSolved = 0;
    private bool itisWar = false; // SET TO FALSE WHEN DONE DEBUGGING
    private bool ticking = true;
    private bool isAdding = true;
    private bool isSubtracting = false;
    protected bool wehavebeenDestroyed = false;

    // Music
    private bool playing;
    private float defaultgamemusicVolume;

    private void Awake()
    {
        _moduleId = _moduleIdCounter++;
        GetComponent<KMBombModule>().OnActivate += OnActivate;
    }

    private void Start()
    {
        if (GetMissionID() == "mod_WAR_WAR")
        {
            itisWar = true;
        }
    }

    private void OnActivate()
    {
        if (itisWar)
        {
            WarPreparations();
        }

        else
        {
            currentTime = 0;
        }
    }

    private void Update()
    {
        if (ticking)
        {
            TimeSpan t = TimeSpan.FromSeconds(currentTime);
            TimerText.text = string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
        }

        if (itisWar)
        {
            if (BombInfo.GetSolvedModuleNames().Count > modulesSolved)
            {
                modulesSolved++;
                StartCoroutine(AddTime());
            }

            if (!isAdding && !isSubtracting)
            {
                StartCoroutine(SubtractTime());
            }

            if (currentTime == 0)
            {
                itisWar = false;
                wehavebeenDestroyed = true;
                StartCoroutine(NuclearBombardment());
            }

            if (BombInfo.GetModuleNames().Count - 1 == modulesSolved)
            {
                itisWar = false;
                Module.HandlePass();
            }
        }
    }

    private void WarPreparations()
    {
        try
        {
            defaultgamemusicVolume = GameMusicControl.GameMusicVolume;
            if (!playing)
            {
                Music.Play();
                playing = true;
            }
        }
        catch (Exception) { }

        try { GameMusicControl.GameMusicVolume = 0.0f; } catch (Exception) { }

        GetComponent<KMGameInfo>().OnStateChange += state => {
            if (state == KMGameInfo.State.Transitioning)
            {
                playing = false;
                try
                {
                    Music.Stop();
                }
                catch
                {
                    throw new Exception("EAAOOO");
                }
                finally
                {
                    GameMusicControl.GameMusicVolume = defaultgamemusicVolume;
                }
            }
        };

        StartCoroutine(AddTime());

    }

    private IEnumerator AddTime()
    {
        isAdding = true;
        Audio.PlaySoundAtTransform("addTime", transform);
        TimerText.color = new Color32(90, 223, 117, 255);
        for (int i = 0; i < 60; i++)
        {
            yield return new WaitForSeconds(0.016f);
            currentTime++;
        }
        TimerText.color = new Color32(255, 0, 0, 255);
        yield return new WaitForSeconds(0.5f);
        isAdding = false;
    }

    private IEnumerator SubtractTime()
    {
        isSubtracting = true;
        Audio.PlaySoundAtTransform("beep", transform);
        yield return new WaitForSeconds(1);
        currentTime--;
        isSubtracting = false;
    }

    private IEnumerator NuclearBombardment()
    {
        while (wehavebeenDestroyed)
        {
            Module.HandleStrike();
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    private string GetMissionID()
    {
        try
        {
            Component gameplayState = GameObject.Find("GameplayState(Clone)").GetComponent("GameplayState");
            Type type = gameplayState.GetType();
            FieldInfo fieldMission = type.GetField("MissionToLoad", BindingFlags.Public | BindingFlags.Static);
            return fieldMission.GetValue(gameplayState).ToString();
        }

        catch (NullReferenceException)
        {
            return "undefined";
        }
    }
}
