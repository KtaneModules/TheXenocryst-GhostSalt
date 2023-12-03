using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class XenocrystScript : MonoBehaviour
{

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

    private Coroutine BlockAnimCoroutine;
    private string[] SubmissionCases = { "[.]", "[.", ".]", "].", ".[", "][.", ".][", "][.]", "[.][" };
    private string ColourLetters = "ROYGBIV", FinalAnswer, GivenAnswer = "", SolvedText = "SOLVED";
    private int[,] Table = { { 1, 2, 3, 4, 5, 6, 7 }, { 8, 9, 1, 2, 3, 4, 5 }, { 6, 7, 8, 9, 1, 2, 3 }, { 4, 5, 6, 5, 4, 5, 6 }, { 7, 8, 9, 1, 2, 3, 4 }, { 5, 6, 7, 8, 9, 1, 2 }, { 3, 4, 5, 6, 7, 8, 9 } };
    private int[] FiveChosenColours = { -1, -1, -1, -1, -1 };
    private int[] Outputs = new int[10];
    private bool BlockGone, ColourblindEnabled, Gone, Solved;
    private float RotationSpeed, RotationSpeedMorePoggers;
    private Color[] Colours = { new Color(1f, 0f, 0f), new Color(1f, 0.5f, 0f), new Color(1f, 1f, 0f), new Color(0f, 1f, 0f), new Color(0f, 0.75f, 1f), new Color(0.25f, 0f, 1f), new Color(0.75f, 0f, 1f), };
    private KMAudio.KMAudioRef AmbientSound, InteractionSound;

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        while (Outputs.Distinct().Count() < 4)
            for (int i = 0; i < 10; i++)
                Outputs[i] = Rnd.Range(0, 7);
        Calculate();
        Inner.material.color = new Color(1f, 1f, 1f);
        BlackHole.transform.localScale = new Vector3(0f, 0f, 0f);
        Block.transform.localPosition += new Vector3(0f, -0.05f, 0f);
        Block.OnInteract += delegate { BlockPress(); return false; };
        Block.OnInteractEnded += delegate { BlockRelease(); };
        Module.OnActivate += delegate { StartCoroutine(OnActivate()); };
        RotationSpeed = Rnd.Range(0.1f, 0.5f);
        RotationSpeedMorePoggers = Rnd.Range(0.5f, 1f);
        ColourblindEnabled = Colourblind.ColorblindModeActive;
        Bomb.OnBombExploded += delegate
        {
            try
            {
                AmbientSound.StopSound();
                AmbientSound = null;
            }
            catch (Exception) { }
        };
    }

    // Life goals here
    void Update()
    {
        if (!ColourblindEnabled)
            ColourblindText.transform.localScale = new Vector3(0, 0, 0);
        else
            ColourblindText.transform.localScale = new Vector3(1f, 1f, 1f);
    }

    void BlockPress()
    {
        if (!GivenAnswer.Contains('['))
            AmbientSound = Audio.PlaySoundAtTransformWithRef("input", BlackHole.transform);
        GivenAnswer += "[";
        if (BlockAnimCoroutine != null)
            StopCoroutine(BlockAnimCoroutine);
        BlockAnimCoroutine = StartCoroutine(BlockPressAnim());
        if (InteractionSound != null)
            InteractionSound.StopSound();
        InteractionSound = Audio.HandlePlaySoundAtTransformWithRef("hold", Block.transform, false);
        Block.AddInteractionPunch(0.5f);
    }

    void BlockRelease()
    {
        GivenAnswer += "]";
        if (BlockAnimCoroutine != null)
            StopCoroutine(BlockAnimCoroutine);
        BlockAnimCoroutine = StartCoroutine(BlockReleaseAnim());
        if (InteractionSound != null)
            InteractionSound.StopSound();
        InteractionSound = Audio.HandlePlaySoundAtTransformWithRef("release", Block.transform, false);
    }

    void Calculate()
    {
        Debug.LogFormat("[The Xenocryst #{0}] The colours which flashed are {1}.", _moduleID, Outputs.Select(x => ColourLetters[x]).Join(", "));
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 10; j++)
                if (!Outputs.Skip(j + 1).Contains(Outputs[j]) && !FiveChosenColours.Contains(Outputs[j]))
                {
                    FiveChosenColours[i] = Outputs[j];
                    j = 10;
                }
        int Fifth = 0;
        for (int i = 0; i < 10; i++)
            if (!FiveChosenColours.Contains(Outputs[i]))
                Fifth += i + 1;
        FiveChosenColours[4] = Outputs[(Fifth + 9) % 10];
        Debug.LogFormat("[The Xenocryst #{0}] The five selected colours are {1}.", _moduleID, FiveChosenColours.Select(x => ColourLetters[x]).Join(", "));
        string SubmitCache = "";
        int CurrentDigitalRoot = 0;
        for (int i = 0; i < 4; i++)
            for (int j = i + 1; j < 5; j++)
            {
                CurrentDigitalRoot += Table[FiveChosenColours[i], FiveChosenColours[j]];
                while (CurrentDigitalRoot > 9)
                    CurrentDigitalRoot -= 9;
                SubmitCache += CurrentDigitalRoot;
            }
        Debug.LogFormat("[The Xenocryst #{0}] After converting the colour pairs to numbers and applying digital roots to them accordingly, the values obtained are {1}.", _moduleID, SubmitCache.ToCharArray().Join(", "));
        for (int i = 0; i < 9; i++)
            SubmitCache = SubmitCache.Replace((i + 1).ToString(), SubmissionCases[i]);
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

    private IEnumerator BlockPressAnim(float duration = 0.075f)
    {
        float timer = 0;
        while (timer < duration)
        {
            Block.transform.localScale = Vector3.one * Mathf.Lerp(0.08f, 0.075f, timer / duration);
            yield return null;
            timer += Time.deltaTime;
        }
        Block.transform.localScale = Vector3.one * 0.075f;
    }

    private IEnumerator BlockReleaseAnim(float duration = 0.075f)
    {
        float timer = 0;
        while (timer < duration)
        {
            Block.transform.localScale = Vector3.one * Mathf.Lerp(0.075f, 0.08f, timer / duration);
            yield return null;
            timer += Time.deltaTime;
        }
        Block.transform.localScale = Vector3.one * 0.08f;
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
        Outer.transform.localEulerAngles = new Vector3(0, 0, Rnd.Range(0, 360));
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
                BlackHole.GetComponent<SpriteRenderer>().color += new Color32(1, 0, 0, 0);
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
            Outer.transform.localEulerAngles += new Vector3(0, 0, -1 * RotationSpeed);
            if (!Solved)
            {
                Block.transform.localEulerAngles += new Vector3(0, RotationSpeed / 4, 0f);
                if (!BlockGone)
                    StatusLight.transform.localEulerAngles += new Vector3(0, RotationSpeedMorePoggers, 0f);
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

    private IEnumerator Flashes(float interval = 1.25f)
    {
        ColourblindText.color = new Color32(0, 0, 0, 0);
        float timer = 0;
        while (timer < interval)
        {
            yield return null;
            timer += Time.deltaTime;
        }
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
                if (!Solved)
                {
                    timer = 0;
                    while (timer < interval)
                    {
                        ColourblindText.color = Color.Lerp(Color.black, Color.clear, timer / interval);
                        Inner.material.color = new Color(Mathf.Lerp(Colours[Outputs[i]].r, 1f, timer / interval), Mathf.Lerp(Colours[Outputs[i]].g, 1f, timer / interval), Mathf.Lerp(Colours[Outputs[i]].b, 1f, timer / interval));
                        yield return null;
                        timer += Time.deltaTime;
                    }
                    ColourblindText.color = Color.clear;
                    Inner.material.color = Color.white;
                }
                ColourblindText.color = new Color(0, 0, 0, 0);
            }
            StartCoroutine(FlashBlack());
            if (GivenAnswer != "..........")
            {
                if (GivenAnswer == FinalAnswer)
                {
                    Debug.LogFormat("[The Xenocryst #{0}] You tried to submit \"{1}\", which was correct. Poggers!", _moduleID, GivenAnswer);
                    while (GivenAnswer.Count(x => x == '[') > GivenAnswer.Count(x => x == ']'))
                        yield return null;
                    StartCoroutine(Solve());
                }
                else
                {
                    Debug.LogFormat("[The Xenocryst #{0}] You tried to submit \"{1}\", which was incorrect. Strike!", _moduleID, GivenAnswer);
                    while (GivenAnswer.Count(x => x == '[') > GivenAnswer.Count(x => x == ']'))
                        yield return null;
                    StartCoroutine(Strike());
                }
            }
            else
                GivenAnswer = "";
            timer = 0;
            while (timer < interval)
            {
                yield return null;
                timer += Time.deltaTime;
            }
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
        try
        {
            AmbientSound.StopSound();
            AmbientSound = null;
        }
        catch (Exception) { }
        StatusLight.GetComponent<MeshRenderer>().material = Mats[1];
        for (int i = 0; i < 60; i++)
        {
            StatusLight.GetComponent<MeshRenderer>().material.color = new Color(Mathf.Lerp(1f, 0, i / 60f), 0, 0, 0.5f);
            yield return new WaitForSeconds(0.01f);
        }
        StatusLight.GetComponent<MeshRenderer>().material = Mats[0];
    }

    private IEnumerator Solve(float cubeShrinkDur = 1.5f, float statusAbsorbDur = 1.25f, float bgAbsorbDur = 0.75f, float disappearDur = 0.1f)
    {
        Module.HandlePass();
        try
        {
            AmbientSound.StopSound();
            AmbientSound = null;
        }
        catch (Exception) { }
        Solved = true;
        StatusLight.GetComponent<MeshRenderer>().material = Mats[2];
        Audio.PlaySoundAtTransform("solve", BlackHole.transform);
        Highlight.transform.localScale = new Vector3(0f, 0f, 0f);
        var randAngle = Rnd.Range(0, Mathf.PI * 2);
        float timer = 0;
        while (timer < cubeShrinkDur)
        {
            Block.transform.localScale = Vector3.one * 0.08f * Easing.InSine(timer, 1f, 0, cubeShrinkDur);
            Block.transform.localRotation = Quaternion.Euler(new Vector3(Mathf.Sin(randAngle), 0, Mathf.Cos(randAngle)) * 360 * Time.deltaTime * (timer / cubeShrinkDur)) * Block.transform.localRotation;
            yield return null;
            timer += Time.deltaTime;
        }
        Block.transform.localScale = Vector3.zero;
        BlockGone = true;
        randAngle = Rnd.Range(0, Mathf.PI * 2);
        var original = StatusLight.transform.localPosition;
        timer = 0;
        while (timer < statusAbsorbDur)
        {
            StatusLight.transform.localScale = Vector3.one * 0.015f * Easing.InSine(timer, 1f, 0, statusAbsorbDur);
            StatusLight.transform.localRotation = Quaternion.Euler(new Vector3(Mathf.Sin(randAngle), 0, Mathf.Cos(randAngle)) * 360 * Time.deltaTime * (timer / statusAbsorbDur)) * StatusLight.transform.localRotation;
            StatusLight.transform.localPosition = new Vector3(Easing.InSine(timer, original.x, 0, statusAbsorbDur), Easing.InSine(timer, original.y, 0.015f, statusAbsorbDur), Easing.InSine(timer, original.z, 0, statusAbsorbDur));
            yield return null;
            timer += Time.deltaTime;
        }
        StatusLight.transform.localScale = Vector3.zero;
        var randDir = 180f;
        randDir += 2 * (Rnd.Range(0, 2) - 0.5f) * 60f;
        timer = 0;
        while (timer < bgAbsorbDur)
        {
            PuzzleBG.transform.localScale = Vector3.one * Easing.InSine(timer, 1f, 0f, bgAbsorbDur);
            PuzzleBG.transform.localEulerAngles = new Vector3(0, Easing.InSine(timer, 180, randDir, bgAbsorbDur), 0);
            yield return null;
            timer += Time.deltaTime;
        }
        PuzzleBG.transform.localScale = Vector3.zero;
        timer = 0;
        while (timer < 0.5f)
        {
            yield return null;
            timer += Time.deltaTime;
        }
        timer = 0;
        while (timer < disappearDur)
        {
            BlackHole.transform.localScale = Vector3.Lerp(Vector3.one * 0.0075f, Vector3.zero, timer / disappearDur);
            yield return null;
            timer += Time.deltaTime;
        }
        BlackHole.transform.localScale = Vector3.zero;
        Gone = true;
        for (int i = 0; i < 6; i++)
        {
            Text.text += SolvedText[i];
            Audio.PlaySoundAtTransform("type", BlackHole.transform);
            timer = 0;
            while (timer < 1 / 6f)
            {
                yield return null;
                timer += Time.deltaTime;
            }
        }
        Audio.PlaySoundAtTransform("ding", BlackHole.transform);
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
            yield return null;
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