using LPCFramework;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.UI.Dropdown;

class AvatarEditor : MonoBehaviour
{
    public GameObject part_ui_prefab;
    private Animation animatorController;
    public List<Button> parts_caption = new List<Button>();
    public List<Dropdown> parts_dropDown = new List<Dropdown>();
    public List<Button> parts_resetBtn = new List<Button>();
    public AvatarTemplete templete;
    public GameObject skeleton;
    private LPCFramework.Avatar avatar;
    private bool change_dirty;

    private void OnEnable()
    {
        avatar = skeleton.GetComponent<LPCFramework.Avatar>();
        if (avatar == null)
        {
            avatar = skeleton.AddComponent<LPCFramework.Avatar>();
        }
        avatar.parts_templete = templete.parts_templete;
        animatorController = skeleton.GetComponent<Animation>();
        UpdateUI();
        PlayIdle();
    }

    void UpdateUI()
    {
        for (int i = 0; i < templete.parts_templete.Length; i++)
        {
            if (parts_dropDown.Count - 1 < i)
            {
                GameObject go_ui = Instantiate(part_ui_prefab);
                go_ui.transform.SetParent(part_ui_prefab.transform.parent);
                var caption = go_ui.transform.Find(parts_caption[0].name);
                parts_caption.Add(caption.GetComponent<Button>());
                var dropDown = go_ui.transform.Find(parts_dropDown[0].name);
                parts_dropDown.Add(dropDown.GetComponent<Dropdown>());
                var resetBtn = go_ui.transform.Find(parts_resetBtn[0].name);
                parts_resetBtn.Add(resetBtn.GetComponent<Button>());
            }
            AvatarPartTemplete part = templete.parts_templete[i];
            DirectoryInfo dir = new DirectoryInfo("Assets/" + part.default_path);
            FileInfo[] files = dir.GetFiles(part.pattern);
            parts_dropDown[i].ClearOptions();
            parts_caption[i].GetComponentInChildren<Text>().text = part.part_name;
            if (files.Length > 0)
            {
                List<OptionData> options = new List<OptionData>();
                for (int f = 0; f < files.Length; f++)
                {
                    var text = Path.GetFileNameWithoutExtension(files[f].Name);
                    options.Add(new OptionData(text));
                }
                parts_dropDown[i].AddOptions(options);
                parts_dropDown[i].onValueChanged.AddListener(OnPartChanged);
                parts_resetBtn[i].onClick.AddListener(OnPartReset);
            }
        }
        change_dirty = true;
    }

    private void LateUpdate()
    {
        if (change_dirty)
        {
            OnPartChanged(0);
            change_dirty = false;
        }
    }

    public void OnPartChanged(int val)
    {
        for (int i = 0; i < templete.parts_templete.Length; i++)
        {
            AvatarPartTemplete part_templ = templete.parts_templete[i];
            avatar.ChangeEquip(part_templ.templete_id, part_templ.default_path + parts_dropDown[i].captionText.text + ".prefab");
        }
    }

    public void OnPartReset()
    {
        GameObject obj = EventSystem.current.currentSelectedGameObject;
        for (int i = 0; i < templete.parts_templete.Length; i++)
        {
            if (obj == parts_resetBtn[i].gameObject)
            {
                avatar.ResetEquip(templete.parts_templete[i].templete_id);
                break;
            }
        }
    }

    public void PlayAnim()
    {
        if (animatorController == null)
            return;
        animatorController.wrapMode = WrapMode.Once;
        animatorController.Play("attack1");
        animatorController.PlayQueued("attack2");
        animatorController.PlayQueued("attack3");
        animatorController.PlayQueued("attack4");
        animatorController.PlayQueued("breath");
    }

    public void PlayIdle()
    {
        if (animatorController == null)
            return;
        animatorController.wrapMode = WrapMode.Loop;
        animatorController.Play("breath");
    }
}

