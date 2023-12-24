using SFB;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class EqualizerController : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] private float _multiplierForHeight;
    [SerializeField] private float _multiplierForLerp;
    [SerializeField] private float _maxHeightScale;
    [SerializeField] private int _samplesCount;

    [Header("Elements")]
    [SerializeField] private GameObject _columnPrefab;
    [SerializeField] private GameObject _scrollContent;
    [SerializeField] private GameObject _scrollView;
    [SerializeField] private Scrollbar _musicScrollbar;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TMP_Dropdown _trackDropdown;
    [SerializeField] private Scrollbar _columnCountScrollbar;
    [SerializeField] private TMP_Dropdown _typeDropdown;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;

    private List<GameObject> _columns = new();
    private List<AudioClip> _clips = new();
    private bool _isPlaying = false;
    private float _savedColumnCount = 0f;
    private int _savedMusicIndex = -1;
    private int _savedType = 0;

    private void Start()
    {
        if (!PlayerPrefs.HasKey("CustomMusic"))
            PlayerPrefs.SetString("CustomMusic", "");
        else
        {
            string[] paths = PlayerPrefs.GetString("CustomMusic").Split("---");
            foreach (string path in paths)
                if(path != "")
                    StartCoroutine(LoadAudio(path));
        }

        AudioClip[] clips = Resources.LoadAll<AudioClip>("Audio");
        foreach (AudioClip clip in clips)
            _clips.Add(clip);

        UpdateDropdown();

        _musicScrollbar.onValueChanged.AddListener(OnMusicScrollbarChange);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();

        if (_isPlaying)
        {
            float[] spectrumData = new float[_samplesCount];
            _audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

            if(_savedType == 0)
            {
                for (int i = 0; i < (int)(_savedColumnCount * _samplesCount) - 1; i++)
                {
                    float amplitude = Mathf.Clamp(spectrumData[i] * _multiplierForHeight, 0f, _maxHeightScale);
                    _columns[i].transform.localScale = Vector3.Lerp(_columns[i].transform.localScale, new Vector3(1f, amplitude, 1f), _multiplierForLerp);
                }
            }

            if (_savedType == 1)
            {
                int border = (int)(_savedColumnCount * _samplesCount) - 1;
                for (int i = 0; i < border; i++)
                {
                    float amplitude = Mathf.Clamp(spectrumData[i] * _multiplierForHeight, 0f, _maxHeightScale);
                    _columns[border - i - 1].transform.localScale = Vector3.Lerp(_columns[border - i - 1].transform.localScale, new Vector3(1f, amplitude, 1f), _multiplierForLerp);
                }
            }

            if (_savedType == 2)
            {
                int border = (int)(_savedColumnCount * _samplesCount) - 1;
                for (int i = 0; i < border; i++)
                {
                    int index1 = (i + border / 2) % border;
                    int index2 = border - index1 - 1;

                    float amplitude = Mathf.Clamp(spectrumData[i] * _multiplierForHeight, 0f, _maxHeightScale);
                    _columns[index1].transform.localScale = Vector3.Lerp(_columns[index1].transform.localScale, new Vector3(1f, amplitude, 1f), _multiplierForLerp);
                    _columns[index2].transform.localScale = Vector3.Lerp(_columns[index2].transform.localScale, new Vector3(1f, amplitude, 1f), _multiplierForLerp);
                }
            }

            _musicScrollbar.value = _audioSource.time / _audioSource.clip.length;
        }
    }

    private void UpdateDropdown()
    {
        _trackDropdown.ClearOptions();
        List<TMP_Dropdown.OptionData> options = new();
        foreach (AudioClip clip in _clips)
            options.Add(new TMP_Dropdown.OptionData(clip.name));
        _trackDropdown.AddOptions(options);
    }
    private void UpdateTrackName()
    {
        _nameText.text = _audioSource.clip.name;
    }

    private void PlayTrack(int index)
    {
        _audioSource.Stop();
        if(index != _savedMusicIndex)
            _audioSource.time = 0f;
        _savedMusicIndex = index;
        _audioSource.clip = _clips[index];
        UpdateTrackName();
        _audioSource.Play();
    }

    private void OnMusicScrollbarChange(float value)
    {
        _audioSource.time = _audioSource.clip.length * value;
    }

    public void ChangeSettings()
    {
        _isPlaying = false;

        int childCount = _columns.Count;
        for (int i = 0; i < childCount; i++)
            Destroy(_columns[i]);
        _columns.Clear();

        _savedColumnCount = _columnCountScrollbar.value;
        _savedType = _typeDropdown.value;

        for (int i = 0; i < (int)(_savedColumnCount * _samplesCount) - 1; i++)
        {
            float width = _scrollView.GetComponent<RectTransform>().sizeDelta.x / ((int)(_savedColumnCount * _samplesCount) - 1);
            GameObject curColumn = Instantiate(_columnPrefab, _scrollContent.transform);
            curColumn.GetComponent<RectTransform>().sizeDelta = new Vector2(width, _columnPrefab.GetComponent<RectTransform>().sizeDelta.y);
            _columns.Add(curColumn);
        }

        PlayTrack(_trackDropdown.value);
        UpdateTrackName();
        _isPlaying = true;
    }

    public void LoadFile()
    { 
        StandaloneFileBrowser.OpenFilePanelAsync("Open File", "", "mp3", false, (string[] paths) => {
            if (paths.Length > 0)
            {
                string filePath = paths[0];
                PlayerPrefs.SetString("CustomMusic", PlayerPrefs.GetString("CustomMusic") + "---" + filePath);
                StartCoroutine(LoadAudio(filePath));
            }
        });
    }
    IEnumerator LoadAudio(string filePath)
    {
        using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file:///" + filePath, AudioType.MPEG);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Ошибка загрузки аудиофайла: " + www.error);
            yield break;
        }

        AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
        audioClip.name = Path.GetFileNameWithoutExtension(filePath);
        _clips.Add(audioClip);
        _trackDropdown.options.Add(new TMP_Dropdown.OptionData(audioClip.name));
    }
}
