using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : LivingEntity
{
    public enum State { Idle, Chasing, Attacking};
    private NavMeshAgent navMeshAgent;
    private Transform target;

    #region Attack
    [SerializeField] private float attackDistanceThreshold = 2.5f;
    private float timeBtwAttack = 1.0f;

    private float nextAttackTime;
    public State currentState;
    public float damage = 1.0f;
    #endregion

    [SerializeField] private float updateRate;

    [SerializeField] private bool hasTarget;

    public ParticleSystem deathEffect;

    Material skinMaterial;
    Color originalColor;

    public static event Action onDeathStatic;//为了记分而创建的事件

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();

        if (GameObject.FindGameObjectWithTag("Player") != null)//因为之后敌人生成时，很可能Player已经阵亡了，首先要判断限制
        {
            hasTarget = true;
            target = GameObject.FindGameObjectWithTag("Player").transform;//target = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        }
    }

    protected override void Start()
    {
        base.Start();

        if (hasTarget)//因为之后敌人生成时，很可能Player已经阵亡了，首先要判断限制
        {
            currentState = State.Chasing;
            target.GetComponent<LivingEntity>().onDeath += TargetDeath;//target.GetComponent<PlayerController>().onDeath += TargetDeath;

            StartCoroutine(UpdatePath());
        }
    }

    #region Attack
    private void Update()
    {
        //navMeshAgent.SetDestination(target.position);//OPTIONAL 锲而不舍类

        if(hasTarget)
        {
            if (Time.time > nextAttackTime)
            {
                float sqrtDstToTarget = (target.position - transform.position).sqrMagnitude;//两者距离的平方
                if (sqrtDstToTarget < Mathf.Pow(attackDistanceThreshold, 2))
                {
                    nextAttackTime = Time.time + timeBtwAttack;
                    StartCoroutine(Attack());
                }
            }
        }
    }

    IEnumerator Attack()
    {
        currentState = State.Attacking;
        navMeshAgent.enabled = false;//保证在【正在攻击的状态】下不会出现继续寻路的情况

        Vector3 originalPos = transform.position;
        Vector3 attackPos = target.position;

        float attackSpeed = 3;//Attack Animation Speed
        float percent = 0;//0->1

        skinMaterial.color = Color.red;
        bool hasAttacked = false;

        while(percent <= 1)
        {
            if(percent >= 0.5f && hasAttacked == false)
            {
                hasAttacked = true;
                target.GetComponent<IDamageable>().TakenDamage(damage);
                FindObjectOfType<GameUI>().UpdateHealth();//MARKER 血条扣血
                Debug.Log("Attack");
            }

            //这一步其实做的是：将攻击动画速度，由attackSpeed控制，由percent限制在0-1之间
            percent += Time.deltaTime * attackSpeed;

            //CORE 这里使用 y = 4 ( -x^2 + x ) x轴为时间，Y轴为移动攻击的距离
            float interpolation = 4 * (-percent * percent + percent);//对照公式
            transform.position = Vector3.Lerp(originalPos, attackPos, interpolation);

            yield return null;
        }

        skinMaterial.color = originalColor;
        currentState = State.Chasing;
        navMeshAgent.enabled = true;
    }
    #endregion

    IEnumerator UpdatePath()//OPTIONAL 摸鱼类
    {
        while (hasTarget)
        {
            if(currentState == State.Chasing)
            {
                if (isDead == false)
                {
                    Vector3 preTargetPos = new Vector3(target.position.x, 0, target.position.z);
                    navMeshAgent.SetDestination(preTargetPos);
                }
            }
            yield return new WaitForSeconds(updateRate);
        }
    }

    private void TargetDeath()
    {
        hasTarget = false;
        currentState = State.Idle;
    }

    public override void TakeHit(float _damageAmount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (_damageAmount >= health)
        {
            if(onDeathStatic != null)
            {
                onDeathStatic();
            }
            GameObject spawnEffect = Instantiate(deathEffect.gameObject, hitPoint, Quaternion.FromToRotation(Vector3.forward, hitDirection));
            Destroy(spawnEffect, deathEffect.startLifetime);//1.0f是PS的Start Lifetime数值
        }
        base.TakeHit(_damageAmount, hitPoint, hitDirection);
    }

    public void SetDifficulty(float _speed, float _damage, float _health, Color _color)
    {
        navMeshAgent.speed = _speed;

        damage = _damage;
        maxHealth = _health;

        deathEffect.startColor = new Color(_color.r, _color.g, _color.b, 1);
        skinMaterial = GetComponent<MeshRenderer>().material;
        skinMaterial.color = _color;
        originalColor = skinMaterial.color;
    }

}
