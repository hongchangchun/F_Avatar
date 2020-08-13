using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LPCFramework
{
    public enum AVATAR_MODE
    {
        MESH,  //挂接上去的，比如武器，一般没有骨骼
        SKINNED_MESH, //替换上去的，比如衣服，一般有骨骼绑定
        COMPLEX = 0xFF, //组合
    }
    //前提：默认装备做在初始模型里面，不需要通过配置去拼装
    [Serializable]
    public class AvatarPartTemplete
    {
        //模板的id
        public int templete_id;
#if UNITY_EDITOR
        //名称，可视化用
        public string part_name;
        //默认目录
        public string default_path;
        //筛选匹配
        public string pattern;
#endif
        //挂节点(有可能是空节点，也可能是mesh)，比如武器对应hand_r，衣服对应body
        public string node_name;
        //换装的功能类型（不是装备类型）
        public AVATAR_MODE mode;
        //资源内的节点名称(这样做的目的是让双手武器之类的可以做在一个预设里，方便管理)
        public string res_node;
        //双手武器之类的，用组合来实现，组合永远不需要再次嵌套
        public int[] sub_templetes;
    }
    //整套templete
    public class AvatarTemplete : MonoBehaviour
    {
        public AvatarPartTemplete[] parts_templete;
    }


}
