using MixFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.UI;

/// <summary>
/// 控制所有名字板的位置，角度，缩放
/// </summary>
public class NameboardMgr : SingletonMono<NameboardMgr>
{

    //使用NativeList，一定要记得Dispose
    private NativeList<NameBoardItemStruct> m_NameBoardItemStructNativeList; //job正在工作的名字板
    //一次工作完成了后，需要继续添加/删除的名字板,使用list时为了保持add，remove的顺序
    //TOOD，可以使用应用池，防止new对象
    List<ToDoNameboard> m_listToDoNameboard; 
    List<Nameboard> m_listNameboard; //当前job处理的名字板，索引对应job
    private bool m_JobComplete = false;
    private List<Transform> m_listActorTransform;

    private NativeList<Vector3> m_Position;
    private NativeList<Vector3> m_Rotation;
    private NativeList<float> m_Scale;
    Camera m_camera;
    private Unity.Jobs.JobHandle m_UpdateNameBoardPositionHandler;
    Dictionary<int, int> m_dicActorIDIdx;
    int m_nameboardJobIdx = 0;
    private void Awake()
    {
        
    }

    public override void Init()
    {
        base.Init();
        DataInit();
    }

    void DataInit()
    {
        m_camera = Camera.main;
        m_listActorTransform = new List<Transform>(64);
        m_Position = new NativeList<Vector3>(128, Allocator.Persistent);
        m_Rotation = new NativeList<Vector3>(128, Allocator.Persistent);
        m_Scale = new NativeList<float>(128, Allocator.Persistent);
        m_listNameboard = new List<Nameboard>(64);
        m_listToDoNameboard = new List<ToDoNameboard>(64);
        m_dicActorIDIdx = new Dictionary<int, int>(64);
        m_NameBoardItemStructNativeList = new NativeList<NameBoardItemStruct>(128, Allocator.Persistent);
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void LateUpdate()
    {
        //执行job
        DoJob();
        //job完成，把job的处理结果赋值给名字板
        m_UpdateNameBoardPositionHandler.Complete();
        m_JobComplete = true;
        var length = m_listNameboard.Count;

        for (int i = 0; i < length; i++)
        {
            var nameboard = m_listNameboard[i];
            nameboard.transform.position = m_Position[i];
            nameboard.transform.forward = m_Rotation[i];
            nameboard.transform.localScale = new Vector3( m_Scale[i],m_Scale[i],m_Scale[i]);
        }
        //一次job结束后，添加新的名字板，移除老的名字板
        UpdateParamAfterJob();
    }

    void UpdateParamAfterJob()
    {
        for (int i = 0; i < m_listToDoNameboard.Count; i++)
        {
            var todo = m_listToDoNameboard[i];
            //TODO：由于是延迟加入，需要判断actor是否有效，可见之类
            if (todo.opType == NameboardToDoOpTyp.Add)
            {
                AddNameboard(todo.actor, todo.nameboard);
            }
            else {
                RemoveNameboard(todo.actor, todo.nameboard);
            }
        }
        m_listToDoNameboard.Clear();
    }
    private void OnDestroy()
    {
        m_NameBoardItemStructNativeList.Dispose();
        m_Position.Dispose();
        m_Rotation.Dispose();
        m_Scale.Dispose();
    }
    public void AddNameboard(Actor actor,Nameboard nameboard)
    {
        if (m_JobComplete)
        {
            //任务完成可以加参数
            if (m_dicActorIDIdx.ContainsKey(actor.m_id) == false)
            {
                m_dicActorIDIdx[actor.m_id] = m_nameboardJobIdx;

                m_NameBoardItemStructNativeList.Add(CreateNameboardStruct(actor, nameboard));
                m_listActorTransform.Add(actor.transform);
                m_listNameboard.Add(nameboard);
                m_Position.Add(default);
                m_Rotation.Add(default);
                m_Scale.Add(default);
                m_nameboardJobIdx++;
            }
        }
        else
        {
            //job正在执行，加入到缓存中，等job执行完毕再加入
            ToDoNameboard todo = new ToDoNameboard();
            todo.actor = actor;
            todo.nameboard = nameboard;
            todo.opType = NameboardToDoOpTyp.Add;
            m_listToDoNameboard.Add(todo);
        }
    }

    public void RemoveNameboard(Actor actor,Nameboard nameboard)
    {
        if (m_JobComplete)
        {
            //任务完成可以加参数，采用尾替换移除
            int idx = -1;
            if (m_dicActorIDIdx.TryGetValue(actor.m_id,out idx) == true)
            {
                m_NameBoardItemStructNativeList.RemoveAtSwapBack(idx);
                m_listActorTransform.RemoveAtSwapBack(idx);
                m_listNameboard.RemoveAtSwapBack(idx);
                m_Position.RemoveAtSwapBack(idx);
                m_Rotation.RemoveAtSwapBack(idx);
                m_Scale.RemoveAtSwapBack(idx);
                m_dicActorIDIdx.Remove(actor.m_id);
                m_nameboardJobIdx--;
            }

        }
        else
        {
            //job正在执行，加入到缓存中，等job执行完毕再移除
            ToDoNameboard todo = new ToDoNameboard();
            todo.actor = actor;
            todo.nameboard = nameboard;
            todo.opType = NameboardToDoOpTyp.Remove;
            m_listToDoNameboard.Add(todo);
        }
    }
    public NameBoardItemStruct CreateNameboardStruct( Actor actor,Nameboard nameboard)
    {
        NameBoardItemStruct st;
        st.posActor = actor.transform.position;
        return st;
    }


    void DoJob()
    {
        m_JobComplete = false;
        var length = m_NameBoardItemStructNativeList.Length;
        //更新数据
        for (int i = 0; i < length; i++)
        {
            var nameBoardData = m_NameBoardItemStructNativeList[i];
            nameBoardData.posActor = m_listActorTransform[i].position; 
        }

        var updateNameBoardPositionJob = new UpdateNameBoardPositionJob(m_Position, m_Rotation, m_Scale,
            m_camera.transform.position, m_NameBoardItemStructNativeList,m_camera.transform.forward
            );

        var updateNameBoardPositionHandler = updateNameBoardPositionJob.Schedule(length, 2);
        {
            m_UpdateNameBoardPositionHandler = updateNameBoardPositionHandler;
        }
        JobHandle.ScheduleBatchedJobs();
    }
}

//结构体里只能值类型传递进入NativeList，否则报错ArgumentException: NameBoardItemStruct used in native collection is not blittable, not primitive, or contains a type tagged as NativeContainer
public struct NameBoardItemStruct
{
    public Vector3 posActor;
   
}

//IJobParallelFor 计算数据,最好是取数据计算，最后把数据计算结果返回
//IJobParallelForTransform 直接操作Transform
[BurstCompile]
struct UpdateNameBoardPositionJob : IJobParallelFor
{
    [WriteOnly]
    private NativeList<Vector3> Position;
    [WriteOnly]
    private NativeList<Vector3> Rotation;
    [WriteOnly]
    private NativeList<float> Scale;

    Vector3 posCamera; //摄像机位置
    Vector3 cameraForward; //摄像机方向
    NativeList<NameBoardItemStruct> NameBoardItemStructNativeList; //需要处理的名字板信息
    public UpdateNameBoardPositionJob(NativeList<Vector3> pPosition, NativeList<Vector3> pRotation, NativeList<float> pScale,
        Vector3 pPosCamera,NativeList<NameBoardItemStruct> pNameBoardItemStructNativeList,Vector3 pCameraForward
        )
    {
        Position = pPosition;
        Rotation = pRotation;
        Scale = pScale;
        posCamera = pPosCamera;
        NameBoardItemStructNativeList = pNameBoardItemStructNativeList;
        cameraForward = pCameraForward;
    }

    // 
    public void Execute(int idx)
    {
        var nameBoardData = NameBoardItemStructNativeList[idx];
        //名字板高度
        Vector3 posActor = nameBoardData.posActor;
        posActor.y += 2; 
        Position[idx] = posActor;

        //名字板方向
        Rotation[idx] = cameraForward;

        //名字板的缩放
        float disCamera = Vector3.Distance(posCamera, posActor);
        float maxDis = 10;//超过多远，名字板scale 为0
        float minScale = 0.3f; //名字板最小scale，过小scale 也没意义
        if (disCamera >= maxDis)
        {
            Scale[idx] = 0;
        }
        else if (disCamera >= 0 && disCamera <= maxDis * 0.3)
        {
            Scale[idx] = 1;
        }
        else
        {
            float disScale = (1 - disCamera / maxDis);
            disScale = Mathf.Max(disScale, minScale);
            Scale[idx] = disScale;
        }
    }
}

public enum NameboardToDoOpTyp
{
    Add,
    Remove
}

public class ToDoNameboard
{
    public Actor actor;
    public Nameboard nameboard;
    public NameboardToDoOpTyp opType;
}


