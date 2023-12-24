using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip[] _clips;

    [Header("Elements")]
    [SerializeField] private Scrollbar _scrollbar;
    [SerializeField] private TextMeshProUGUI _nameText;

    private int currentTrackIndex = 0;

    private void Start()
    {
        _scrollbar.onValueChanged.AddListener(OnScrollbarChange);
        PlayTrack(currentTrackIndex);
        UpdateTrackName();
    }

    private void Update()
    {
        _scrollbar.size = _audioSource.time / _audioSource.clip.length;
    }

    private void UpdateTrackName()
    {
        _nameText.text = _audioSource.clip.name;
    }

    private void PlayTrack(int index)
    {
        _audioSource.Stop();
        _audioSource.clip = _clips[index];
        UpdateTrackName();
        _audioSource.Play();
    }

    public void OnScrollbarChange(float value)
    {
        _audioSource.time = _audioSource.clip.length * value;
    }
}
