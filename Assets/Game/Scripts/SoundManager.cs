// 효과음 / 배경음 재생 (어느 씬에서든 SoundManager.Play("이름") 으로 사용)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    static SoundManager _inst;
    public static bool Muted;

    AudioSource sfx, amb, loop;
    readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    public static SoundManager I
    {
        get
        {
            if (_inst == null)
            {
                var go = new GameObject("SoundManager");
                _inst = go.AddComponent<SoundManager>();
                DontDestroyOnLoad(go);
            }
            return _inst;
        }
    }

    void Awake()
    {
        if (_inst != null && _inst != this) { Destroy(gameObject); return; }
        _inst = this;
        sfx = gameObject.AddComponent<AudioSource>();
        amb = gameObject.AddComponent<AudioSource>();
        loop = gameObject.AddComponent<AudioSource>();
        amb.loop = true; loop.loop = true;
        sfx.playOnAwake = amb.playOnAwake = loop.playOnAwake = false;
    }

    AudioClip Clip(string n)
    {
        if (!cache.TryGetValue(n, out var c))
        {
            c = Resources.Load<AudioClip>("Audio/" + n);
            cache[n] = c;
        }
        return c;
    }

    // 효과음 한 번 재생
    public static void Play(string name, float volume = 1f)
    {
        var c = I.Clip(name);
        if (c != null) I.sfx.PlayOneShot(c, volume);
    }

    // 배경음(반복) 재생
    public static void Amb(string name, float volume = 0.6f)
    {
        var c = I.Clip(name);
        if (c == null) return;
        if (I.amb.clip == c && I.amb.isPlaying) { I.amb.volume = volume; return; }
        I.amb.clip = c; I.amb.volume = volume; I.amb.Play();
    }
    public static void StopAmb() { if (_inst != null) _inst.amb.Stop(); }

    // 전화벨처럼 계속 울리는 소리
    public static void Loop(string name, float volume = 0.8f)
    {
        var c = I.Clip(name);
        if (c == null) return;
        I.loop.clip = c; I.loop.volume = volume; I.loop.Play();
    }
    public static void StopLoop() { if (_inst != null) _inst.loop.Stop(); }

    public static void SetMuted(bool m)
    {
        Muted = m;
        AudioListener.volume = m ? 0f : 1f;
    }
}

