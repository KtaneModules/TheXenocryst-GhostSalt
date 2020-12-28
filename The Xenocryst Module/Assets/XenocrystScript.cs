using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class XenocrystScript : MonoBehaviour {

    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMColorblindMode Colourblind;
    public GameObject BlackHole;
    public GameObject Outer;
    public GameObject OuterSecond;
    public GameObject OuterThird;
    public GameObject StatusLight;
    public GameObject Highlight;
    public GameObject PuzzleBG;
    public KMSelectable Block;
    public MeshRenderer Inner;
    public TextMesh Text;
    public TextMesh ColourblindText;
    public Material[] Mats;

    private string[] SubmissionCases = { "[.]", "[.", ".]", "].", ".[", "][.", ".][", "][.]", "[.][" };
    private string SolvedText = "SOLVED";
    private string FinalAnswer;
    private string GivenAnswer;
    private string ColourLetters = "ROYGBIV";
    private int[,] Table = { { 1, 2, 3, 4, 5, 6, 7 }, { 8, 9, 1, 2, 3, 4, 5 }, { 6, 7, 8, 9, 1, 2, 3 }, { 4, 5, 6, 5, 4, 5, 6 }, { 7, 8, 9, 1, 2, 3, 4 }, { 5, 6, 7, 8, 9, 1, 2 }, { 3, 4, 5, 6, 7, 8, 9 } };
    private int[] FiveChosenColours = { -1,-1,-1,-1,-1 };
    private int[] Outputs = new int[10];
    private bool Solved;
    private bool Gone;
    private bool BlockGone;
    private bool ColourblindEnabled;
    private float RotationSpeed;
    private float RotationSpeedMorePoggers;
    private Color[] Colours = { new Color(1f, 0f, 0f), new Color(1f, 0.5f, 0f), new Color(1f, 1f, 0f), new Color(0f, 1f, 0f), new Color(0f, 0.75f, 1f), new Color(0.25f, 0f, 1f), new Color(0.75f, 0f, 1f), };
    private KMAudio.KMAudioRef Sound;

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        while (Outputs.Distinct().Count() < 4)
            for (int i = 0; i < 10; i++)
                Outputs[i] = Rnd.Range(0, 7);
        Calculate();
        Inner.material.color = new Color(1f,1f,1f);
        BlackHole.transform.localScale = new Vector3(0f, 0f, 0f);
        Block.transform.localPosition += new Vector3(0f, -0.05f, 0f);
        Block.OnInteract += delegate { if (!GivenAnswer.Contains('[')) { Sound = Audio.PlaySoundAtTransformWithRef("input", BlackHole.transform); }; GivenAnswer += "["; return false; };
        Block.OnInteractEnded += delegate { GivenAnswer += "]"; };
        Module.OnActivate += delegate { StartCoroutine(OnActivate()); };
        RotationSpeed = Rnd.Range(0.1f,0.5f);
        RotationSpeedMorePoggers = Rnd.Range(0.5f, 1f);
        ColourblindEnabled = Colourblind.ColorblindModeActive;
    }

    // Life goals here
    void Update () {
        if (!ColourblindEnabled)
        {
            ColourblindText.transform.localScale = new Vector3(0, 0, 0);
        }
        else
        {
            ColourblindText.transform.localScale = new Vector3(1f, 1f, 1f);
        }
    }

    void Calculate()
    {
        Debug.LogFormat("[The Xenocryst #{0}] The colours which flashed are {1}.", _moduleID, Outputs.Select(x => ColourLetters[x]).Join(", "));
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                if (!Outputs.Skip(j + 1).Contains(Outputs[j]) && !FiveChosenColours.Contains(Outputs[j]))
                {
                    FiveChosenColours[i] = Outputs[j];
                    j = 10;
                }
            }
        }
        int Fifth = 0;
        for (int i = 0; i < 10; i++)
        {
            if (!FiveChosenColours.Contains(Outputs[i]))
                Fifth += i + 1;
        }
        FiveChosenColours[4] = Outputs[(Fifth + 9) % 10];
        Debug.LogFormat("[The Xenocryst #{0}] The five selected colours are {1}.", _moduleID, FiveChosenColours.Select(x => ColourLetters[x]).Join(", "));
        string SubmitCache = "";
        int CurrentDigitalRoot = 0;
        for (int i = 0; i < 4; i++)
        {
            for (int j = i + 1; j < 5; j++)
            {
                CurrentDigitalRoot += Table[FiveChosenColours[i], FiveChosenColours[j]];
                while (CurrentDigitalRoot > 9)
                    CurrentDigitalRoot -= 9;
                SubmitCache += CurrentDigitalRoot;
            }
        }
        Debug.LogFormat("[The Xenocryst #{0}] After converting the colour pairs to numbers and applying digital roots to them accordingly, the values obtained are {1}.", _moduleID, SubmitCache.ToCharArray().Join(", "));
        for (int i = 0; i < 9; i++)
        {
            SubmitCache = SubmitCache.Replace((i + 1).ToString(),SubmissionCases[i]);
        }
        Debug.LogFormat("[The Xenocryst #{0}] The raw sequence is \"{1}\".", _moduleID, SubmitCache);
        bool Holding = false;
        for (int i = 0; i < SubmitCache.Length; i++)
        {
            switch (SubmitCache[i])
            {
                case '[':
                    if (!Holding)
                    {
                        FinalAnswer += "[";
                        Holding = true;
                    }
                    break;
                case ']':
                    if (Holding)
                    {
                        FinalAnswer += "]";
                        Holding = false;
                    }
                    break;
                default:
                    FinalAnswer += ".";
                    break;
            }
        }
        Debug.LogFormat("[The Xenocryst #{0}] After removing any extraneous interactions, the final sequence is \"{1}\".", _moduleID, FinalAnswer);
    }

    private IEnumerator OnActivate()
    {
        Audio.PlaySoundAtTransform("activate", BlackHole.transform);
        for (int i = 0; i < 5; i++)
        {
            BlackHole.transform.localScale += new Vector3(0.00025f, 0.00025f, 0f);
            yield return new WaitForSeconds(0.01f);
        }
        yield return new WaitForSeconds(0.4f);
        for (int i = 0; i < 5; i++)
        {
            BlackHole.transform.localScale += new Vector3(0.00125f, 0.00125f, 0f);
            yield return new WaitForSeconds(0.01f);
        }
        StartCoroutine(BlackHoleJitter());
        StartCoroutine(Colouring());
        StartCoroutine(Rotate());
        StartCoroutine(Flashes());
        Outer.transform.localEulerAngles = new Vector3(0, 0, Rnd.Range(0,360));
        for (int i = 0; i < 40; i++)
        {
            Block.transform.localPosition += new Vector3(0f, 0.00125f, 0f);
            yield return new WaitForSeconds(0.01f);
        }
    }

    private IEnumerator BlackHoleJitter()
    {
        while (!Gone)
        {
            BlackHole.transform.localScale += new Vector3(-0.0001f, -0.0001f, 0f);
            yield return new WaitForSeconds(0.02f);
            BlackHole.transform.localScale += new Vector3(0.0001f, 0.0001f, 0f);
            yield return new WaitForSeconds(0.02f);
        }
    }

    private IEnumerator Colouring()
    {
        while (true)
        {
            for (int i = 0; i < 255; i++)
            {
                BlackHole.GetComponent<SpriteRenderer>().color -= new Color32(1, 0, 0, 0);
                BlackHole.GetComponent<SpriteRenderer>().color += new Color32(0, 1, 0, 0);
                Outer.GetComponent<SpriteRenderer>().color -= new Color32(1, 0, 0, 0);
                Outer.GetComponent<SpriteRenderer>().color += new Color32(0, 1, 0, 0);
                OuterSecond.GetComponent<SpriteRenderer>().color -= new Color32(1, 0, 0, 0);
                OuterSecond.GetComponent<SpriteRenderer>().color += new Color32(0, 1, 0, 0);
                OuterThird.GetComponent<SpriteRenderer>().color -= new Color32(1, 0, 0, 0);
                OuterThird.GetComponent<SpriteRenderer>().color += new Color32(0, 1, 0, 0);
                yield return new WaitForSeconds(0.01f);
            }
            for (int i = 0; i < 255; i++)
            {
                BlackHole.GetComponent<SpriteRenderer>().color -= new Color32(0, 1, 0, 0);
                BlackHole.GetComponent<SpriteRenderer>().color += new Color32(0, 0, 1, 0);
                Outer.GetComponent<SpriteRenderer>().color -= new Color32(0, 1, 0, 0);
                Outer.GetComponent<SpriteRenderer>().color += new Color32(0, 0, 1, 0);
                OuterSecond.GetComponent<SpriteRenderer>().color -= new Color32(0, 1, 0, 0);
                OuterSecond.GetComponent<SpriteRenderer>().color += new Color32(0, 0, 1, 0);
                OuterThird.GetComponent<SpriteRenderer>().color -= new Color32(0, 1, 0, 0);
                OuterThird.GetComponent<SpriteRenderer>().color += new Color32(0, 0, 1, 0);
                yield return new WaitForSeconds(0.01f);
            }
            for (int i = 0; i < 255; i++)
            {
                BlackHole.GetComponent<SpriteRenderer>().color -= new Color32(0, 0, 1, 0);
                BlackHole.GetComponent<SpriteRenderer>().color += new Color32(1, 0, 0, 0);
                Outer.GetComponent<SpriteRenderer>().color -= new Color32(0, 0, 1, 0);
                Outer.GetComponent<SpriteRenderer>().color += new Color32(1, 0, 0, 0);
                OuterSecond.GetComponent<SpriteRenderer>().color -= new Color32(0, 0, 1, 0);
                OuterSecond.GetComponent<SpriteRenderer>().color += new Color32(1, 0, 0, 0);
                OuterThird.GetComponent<SpriteRenderer>().color -= new Color32(0, 0, 1, 0);
                OuterThird.GetComponent<SpriteRenderer>().color += new Color32(1, 0, 0, 0);
                yield return new WaitForSeconds(0.01f);
            }
        }
    }

    private IEnumerator Rotate()
    {
        while (true)
        {
            Outer.transform.localEulerAngles += new Vector3(0, 0, -1*RotationSpeed);
            if (!Solved)
            {
                Block.transform.localEulerAngles += new Vector3(0, RotationSpeed / 4, 0f);
                if (!BlockGone)
                    StatusLight.transform.localEulerAngles += new Vector3(0, RotationSpeedMorePoggers, 0f);
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

    private IEnumerator Flashes()
    {
        ColourblindText.color = new Color32(0, 0, 0, 0);
        yield return new WaitForSeconds(1f);
        while (!Solved)
        {
            for (int i = 0; i < 10; i++)
            {
                if (!Solved)
                {
                    ColourblindText.color = new Color(0, 0, 0, 1);
                    ColourblindText.text = ColourLetters[Outputs[i]].ToString();
                }
                GivenAnswer += ".";
                for (int j = 0; j < 60; j++)
                {
                    if (!Solved)
                    {
                        ColourblindText.color -= new Color(0, 0, 0, 1/60f);
                        Inner.material.color = new Color(Mathf.Lerp(Colours[Outputs[i]].r, 1f, j / 60f), Mathf.Lerp(Colours[Outputs[i]].g, 1f, j / 60f), Mathf.Lerp(Colours[Outputs[i]].b, 1f, j / 60f));
                        yield return new WaitForSeconds(0.01f);
                    }
                }
                ColourblindText.color = new Color(0, 0, 0, 0);
            }
            StartCoroutine(FlashBlack());
            if (GivenAnswer != "..........")
            {   
                if (GivenAnswer == FinalAnswer)
                {
                    while (GivenAnswer.Count(x => x == '[') > GivenAnswer.Count(x => x == ']'))
                        yield return null;
                    Debug.LogFormat("[The Xenocryst #{0}] You tried to submit \"{1}\", which was correct. Poggers!", _moduleID, GivenAnswer);
                    StartCoroutine(Solve());
                }
                else
                {
                    while (GivenAnswer.Count(x => x == '[') > GivenAnswer.Count(x => x == ']'))
                        yield return null;
                    Debug.LogFormat("[The Xenocryst #{0}] You tried to submit \"{1}\", which was incorrect. Strike!", _moduleID, GivenAnswer);
                    StartCoroutine(Strike());
                }
            }
            else
                GivenAnswer = "";
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator FlashBlack()
    {
        yield return null;
        for (int i = 0; i < 60; i++)
        {
            Inner.material.color = new Color(Mathf.Lerp(0f, 1f, i / 60f), Mathf.Lerp(0f, 1f, i / 60f), Mathf.Lerp(0f, 1f, i / 60f));
            yield return new WaitForSeconds(0.01f);
        }
    }

    private IEnumerator Strike()
    {
        Module.HandleStrike();
        GivenAnswer = "";
        Sound.StopSound();
        Sound = null;
        StatusLight.GetComponent<MeshRenderer>().material = Mats[1];
        for (int i = 0; i < 60; i++)
        {
            StatusLight.GetComponent<MeshRenderer>().material.color = new Color(Mathf.Lerp(1f, 0, i / 60f), 0, 0, 0.5f);
            yield return new WaitForSeconds(0.01f);
        }
        StatusLight.GetComponent<MeshRenderer>().material = Mats[0];
    }

    private IEnumerator Solve()
    {
        Module.HandlePass();
        Sound.StopSound();
        Sound = null;
        Solved = true;
        StatusLight.GetComponent<MeshRenderer>().material = Mats[2];
        Audio.PlaySoundAtTransform("solve", BlackHole.transform);
        StartCoroutine(FlipBlock());
        Highlight.transform.localScale = new Vector3(0f, 0f, 0f);
        for (int i = 0; i < 80; i++)
        {
            Block.transform.localScale += new Vector3(-0.001f, -0.001f, -0.001f);
            yield return new WaitForSeconds(0.01f);
        }
        BlockGone = true;
        for (int i = 0; i < 10; i++)
        {
            StatusLight.transform.localEulerAngles += new Vector3(-5f, 0f, -5f);
            yield return new WaitForSeconds(0.02f);
            StatusLight.transform.localEulerAngles = new Vector3(-90f, 0f, 0f);
            yield return new WaitForSeconds(0.02f);
        }
        StartCoroutine(StatusLightSpin());
        StatusLight.transform.localEulerAngles = new Vector3(-90f, 0f, 0f);
        for (int i = 0; i < 30; i++)
        {
            StatusLight.transform.localPosition += new Vector3(-0.00250556666666666666666666666666f, -8.3666666666666666666666666666667e-4f, -0.00253523333333333333333333333333f);
            StatusLight.transform.localScale += new Vector3(-0.0005f, -0.0005f, -0.0005f);
            yield return new WaitForSeconds(0.01f);
        }
        for (int i = 0; i < 50; i++)
        {
            PuzzleBG.transform.localScale += new Vector3(-0.02f, -0.02f, -0.02f);
            yield return new WaitForSeconds(0.01f);
        }
        yield return new WaitForSeconds(0.25f);
        for (int i = 0; i < 5; i++)
        {
            BlackHole.transform.localScale += new Vector3(-0.0015f, -0.0015f, 0f);
            yield return new WaitForSeconds(0.01f);
        }
        Gone = true;
        for (int i = 0; i < 6; i++)
        {
            Text.text += SolvedText[i];
            Audio.PlaySoundAtTransform("type", BlackHole.transform);
            yield return new WaitForSeconds(0.16666666666666666666666666666667f);
        }
        Audio.PlaySoundAtTransform("ding",BlackHole.transform);
    }

    private IEnumerator FlipBlock()
    {
        for (int i = 0; i < 80; i++)
        {
            Block.transform.localEulerAngles += new Vector3(1.25f, 0f, 1.25f);
            yield return new WaitForSeconds(0.01f);
        }
    }

    private IEnumerator StatusLightSpin()
    {
        for (int i = 0; i < 60; i++)
        {
            StatusLight.transform.localEulerAngles += new Vector3(5f, 5f, 5f);
            yield return new WaitForSeconds(0.01f);
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} [.].' to hold the xenocryst, then wait a pulse, then release it and then wait another pulse. Note that you can only validly send a command which contains 10 '.'s. You can also use '!{0} colo(u)rblind' to toggle colourblind support.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        if (command == null)
        {
            yield return "sendtochaterror Invalid command.";
            yield break;
        }
        if (command == "colourblind" || command == "colorblind" /*ie. the worst spelling of colourblind*/)
        {
            ColourblindEnabled = !ColourblindEnabled;
        }
        else
        {
            int PulseWaits = 0;
            bool Holding = false;
            for (int i = 0; i < command.Length; i++)
            {
                switch (command[i])
                {
                    case '[':
                        if (!Holding)
                        {
                            Holding = true;
                        }
                        else
                        {
                            yield return "sendtochaterror Invalid command.";
                            yield break;
                        }
                        break;
                    case ']':
                        if (Holding)
                        {
                            Holding = false;
                        }
                        else
                        {
                            yield return "sendtochaterror Invalid command.";
                            yield break;
                        }
                        break;
                    case '.':
                        PulseWaits++;
                        break;
                    default:
                        yield return "sendtochaterror Invalid command.";
                        yield break;
                }
            }
            if (PulseWaits != 10)
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }
            if (GivenAnswer == "")
                yield return new WaitForSeconds(1f);
            while (GivenAnswer != "")
                yield return "trycancel Submission for The Xenocryst has been cancelled.";
            for (int i = 0; i < command.Length; i++)
            {
                switch (command[i])
                {
                    case '[':
                        yield return null;
                        Block.OnInteract();
                        break;
                    case ']':
                        yield return null;
                        Block.OnInteractEnded();
                        break;
                    default:
                        int GivenAnswerLength = GivenAnswer.Length;
                        while (GivenAnswerLength == GivenAnswer.Length)
                            yield return null;
                        break;
                }
            }
            if (Holding)
            {
                yield return new WaitForSeconds(2f);
                Block.OnInteractEnded();
            }
        }
    }
    IEnumerator TwitchHandleForcedSolve()
    {
        if (GivenAnswer == "")
            yield return new WaitForSeconds(1f);
        while (GivenAnswer != "")
            yield return true;
        bool Holding = false;
        for (int i = 0; i < FinalAnswer.Length; i++)
        {
            switch (FinalAnswer[i])
            {
                case '[':
                    yield return null;
                    Holding = true;
                    Block.OnInteract();
                    break;
                case ']':
                    yield return null;
                    Holding = false;
                    Block.OnInteractEnded();
                    break;
                default:
                    int GivenAnswerLength = GivenAnswer.Length;
                    while (GivenAnswerLength == GivenAnswer.Length)
                        yield return null;
                    break;
            }
        }
        if (Holding)
        {
            yield return new WaitForSeconds(2f);
            Block.OnInteractEnded();
        }
    }

}