using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace LPCFramework
{
    //如果是组合类型的部件，这个数据应该是展开的
    public class AvatarPartData
    {
        //模板的id
        public int templete_id;
        //路径也保存下来，避免重复换装
        public string res_path;
        //老节点，保存下来，以供恢复
        public GameObject origin_go;
        //新节点，也存下来，供卸载
        public GameObject new_go;
        //标志当前是否在加载
        public bool is_loading;
    }
    
    class ChangeCommand
    {
        public AvatarPartTemplete templete;
        public string res_path;
    }

    public class Avatar : MonoBehaviour
    {
        public bool combine = false;
        public int combine_tex_size = 512;
        //这个templete数据，在编辑器里面提供另外的类来编辑，在此处使用
        public AvatarPartTemplete[] parts_templete;
        //已经换上的装备信息
        private List<AvatarPartData> parts = new List<AvatarPartData>();
        private List<ChangeCommand> change_commands = new List<ChangeCommand>();
        private bool change_dirty = false;

        public bool ChangeEquip(int templeteId, string resPath)
        {
            //Debug.LogFormat("ChangeEquip:{0},{1}", templeteId, resPath);
            AvatarPartTemplete templete = FindTemplete(templeteId);
            if (templete == null)
            {
                Debug.Log("no templete id:" + templeteId);
                return false;
            }
            if (templete.mode == AVATAR_MODE.COMPLEX)
            {
                for (int i = 0; i < templete.sub_templetes.Length; i++)
                {
                    AvatarPartTemplete sub_templete = FindTemplete(templete.sub_templetes[i]);
                    if (sub_templete != null)
                        ChangePart(sub_templete, resPath);
                }
            }
            else
            {
                ChangePart(templete, resPath);
            }
            return true;
        }

        public bool ResetEquip(int templeteId)
        {
            AvatarPartTemplete templete = FindTemplete(templeteId);
            if (templete == null)
            {
                Debug.Log("no templete id:" + templeteId);
                return false;
            }
            if (templete.mode == AVATAR_MODE.COMPLEX)
            {
                for (int i = 0; i < templete.sub_templetes.Length; i++)
                {
                    AvatarPartTemplete sub_templete = FindTemplete(templete.sub_templetes[i]);
                    if (sub_templete != null)
                        ResetPart(sub_templete);
                }
            }
            else
            {
                ResetPart(templete);
            }
            return true;
        }

        private void ChangePart(AvatarPartTemplete templete, string resPath)
        {
            //resPath为空串，表示卸下
            //如果当前在加载，就缓存下来，等加载完再继续
            AvatarPartData part = FindPartData(templete.templete_id);
            if (part != null)
            {
                if (part.res_path == resPath)
                {
                    Debug.LogFormat("ChangePart: duplicate command, ignore({0})", resPath);
                    return;
                }
                if (part.is_loading)
                {
                    Debug.LogFormat("ChangePart delayed, current loading:{0}", resPath);
                    change_commands.Add(new ChangeCommand() { templete = templete, res_path = resPath });
                    return;
                }
                else
                {
                    part.res_path = resPath;
                }
            }
            else
            {
                part = new AvatarPartData() { templete_id = templete.templete_id, res_path = resPath, is_loading = true };
                parts.Add(part);
            }
            if (resPath.Length == 0)
            {
                ChangePart(templete, (GameObject)null);
                return;
            }
            Addressables.LoadAssetAsync<UnityEngine.Object>(resPath).Completed += (AsyncOperationHandle<UnityEngine.Object> res) =>
            {
                switch (res.Status)
                {
                    case AsyncOperationStatus.None:
                        Debug.LogError("attemp to load a none asset:" + name + "," + res.DebugName);
                        break;
                    case AsyncOperationStatus.Failed:
                        Debug.LogError("load asset failed:" + name + "," + res.DebugName);
                        break;
                    case AsyncOperationStatus.Succeeded:
                        ChangePart(templete, (GameObject)Instantiate(res.Result));

                        if (change_commands.Count > 0)
                        {
                            ChangeCommand cmd = change_commands[0];
                            //同一类型的加载完才继续下一个，但有一个副作用，就是队列里的都要再等着
                            if (cmd.templete.templete_id == templete.templete_id)
                            {
                                change_commands.RemoveAt(0);
                                Debug.LogFormat("ChangePart resume, current loading:{0}", cmd.res_path);
                                ChangePart(cmd.templete, cmd.res_path);
                            }
                        }
                        break;
                }
            };
        }

        private bool ChangePart(AvatarPartTemplete templete, GameObject go)
        {
            GameObject res_node = go;
            if (go != null)
            {
                Transform trans = go.transform;
                if (templete.res_node.Length > 0)
                {
                    trans = go.transform.Find(templete.res_node);
                    if (trans != null)
                        res_node = trans.gameObject;
                    else
                    {
                        Debug.LogErrorFormat("ChangePart:Cannot find res_node:{0}", templete.res_node);
                        res_node = go;
                    }
                }
            }
            switch (templete.mode)
            {
                case AVATAR_MODE.MESH:
                    ChangeMeshPart(templete, res_node);
                    break;
                case AVATAR_MODE.SKINNED_MESH:
                    ChangeSkinnedPart(templete, res_node);
                    break;
            }
            return true;
        }

        //go==null 表示卸载吧，下同
        private bool ChangeMeshPart(AvatarPartTemplete templete, GameObject go)
        {
            AvatarPartData part = FindPartData(templete.templete_id);
            //卸载掉老的
            if (part.new_go != null)
            {
                //LPCFramework.GamePoolManager.Instance.ReturnToPool(part.new_go);
                DestroyImmediate(part.new_go);
                part.new_go = null;
            }
            part.is_loading = false;
            var parent = GameObject.Find(templete.node_name);
            if (parent == null)
            {
                Debug.LogError("ChangeAttachPart cannot find node_name:" + templete.node_name);
                return false;
            }
            if (go != null)
            {
                go.transform.SetParent(parent.transform);
            }
            part.new_go = go;
            if (go != null)
            {
                go.transform.localScale = Vector3.one;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
            }
            return true;
        }

        //go==null 表示卸载吧，下同
        private bool ChangeSkinnedPart(AvatarPartTemplete templete, GameObject go)
        {
            AvatarPartData part = FindPartData(templete.templete_id);
            GameObject part_node;
            //卸载掉老的
            if (part.new_go != null)
            {
                //LPCFramework.GamePoolManager.Instance.ReturnToPool(part.new_go);
                DestroyImmediate(part.new_go);
                part.new_go = null;
            }
            part.is_loading = false;
            //如果初始骨骼上带了模型，就卸掉
            part_node = GameObject.Find(templete.node_name);
            if (part_node != null)
            {
                part.origin_go = part_node.gameObject;
                part_node.transform.SetParent(null);
            }
            part.new_go = go;
            if (go == null)
            {
                //参数为null约定为卸下
                //老部件还原
                RemovePartData(templete.templete_id);
            }
            else
            {
                go.SetActive(false);
            }
            change_dirty = true;
            return true;
        }

        private void ResetPart(AvatarPartTemplete templete)
        {
            AvatarPartData part = FindPartData(templete.templete_id);
            ChangePart(templete, "");
        }

        private AvatarPartTemplete FindTemplete(int templeteId)
        {
            for (int i = 0; i < parts_templete.Length; i++)
            {
                if (parts_templete[i].templete_id == templeteId)
                    return parts_templete[i];
            }
            return null;
        }

        private AvatarPartData FindPartData(int templeteId)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].templete_id == templeteId)
                    return parts[i];
            }
            return null;
        }

        private int RemovePartData(int templeteId)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].templete_id == templeteId)
                {
                    parts.RemoveAt(i);
                    return i;
                }
            }
            return -1;
        }

        private void Update()
        {
            if (change_dirty)
            {
                SkinnedMeshRenderer[] smrs = GetComponentsInChildren<SkinnedMeshRenderer>();
                List<SkinnedMeshRenderer> smr_list = new List<SkinnedMeshRenderer>();
                for (int i = 1; i < smrs.Length; i++)
                {   //第一个不加，因为是合并之后的
                    smr_list.Add(smrs[i]);
                }
                for (int i = 0; i < parts.Count; i++)
                {
                    AvatarPartData part = parts[i];
                    if (part.new_go)
                    {
                        var smr = part.new_go.GetComponentInChildren<SkinnedMeshRenderer>();
                        if (smr != null)
                            smr_list.Add(smr);
                    }
                }
                CombineSkinnedMgr.CombineObject(this.gameObject, smr_list.ToArray(), combine, combine_tex_size);
                change_dirty = false;
            }
        }
    }
}
