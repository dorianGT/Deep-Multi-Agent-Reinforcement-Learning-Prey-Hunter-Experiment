using UnityEngine;
using System.Collections.Generic;
using Unity.MLAgents;

/// <summary>
/// Gère l'environnement de jeu pour un scénario de chasse entre chasseurs et proies.
/// Contrôle l'apparition, la réinitialisation et le déroulement des épisodes.
/// </summary>
public class HunterPreyEnv : MonoBehaviour
{
    #region Variables

    [Header("Prefabs & Spawn")]
    public GameObject hunterPrefab;
    public GameObject preyPrefab;
    public Transform spawnPoint;
    public float randomRangeAgent = 3f;

    [Header("Game Settings")]
    public int hunterCount = 2;
    public int preyCount = 3;
    public float timeLimit = 60f;

    private float hidingTime = 5f;
    private bool huntersReleased = false;

    private List<HunterAgent> hunters;
    private List<PreyAgent> preys;
    private List<HunterAgent> disabledHunters;
    private List<PreyAgent> disabledPreys;

    private float timer;
    private int hunterWins = 0;
    private int preyWins = 0;

    private SimpleMultiAgentGroup hunterAgentGroup;
    private SimpleMultiAgentGroup preyAgentGroup;

    #endregion

    void Start()
    {
        SpawnAgents();
        Invoke(nameof(ReleaseHunters), hidingTime);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > timeLimit)
        {
            EndEpisode(true); // Temps écoulé, les proies gagnent
        }
    }

    /// <summary>
    /// Instancie les agents chasseurs et proies et les enregistre dans leurs groupes respectifs.
    /// </summary>
    void SpawnAgents()
    {
        hunters = new List<HunterAgent>();
        preys = new List<PreyAgent>();
        disabledHunters = new List<HunterAgent>();
        disabledPreys = new List<PreyAgent>();

        GameObject tmp;
        HunterAgent hAgent;
        PreyAgent pAgent;
        Vector3 position;

        // Spawn des chasseurs
        for (int i = 0; i < hunterCount; i++)
        {
            position = spawnPoint.position + new Vector3(
                Random.Range(-randomRangeAgent, randomRangeAgent),
                0,
                Random.Range(-randomRangeAgent, randomRangeAgent)
            );
            tmp = Instantiate(hunterPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            hAgent = tmp.GetComponent<HunterAgent>();
            hAgent.SetEnv(this);
            hunters.Add(hAgent);
        }

        // Spawn des proies
        for (int i = 0; i < preyCount; i++)
        {
            position = spawnPoint.position + new Vector3(
                Random.Range(-randomRangeAgent, randomRangeAgent),
                0,
                Random.Range(-randomRangeAgent, randomRangeAgent)
            );
            tmp = Instantiate(preyPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            pAgent = tmp.GetComponent<PreyAgent>();
            pAgent.SetEnv(this);
            preys.Add(pAgent);
        }

        // Création des groupes multi-agents
        hunterAgentGroup = new SimpleMultiAgentGroup();
        preyAgentGroup = new SimpleMultiAgentGroup();

        foreach (var hunter in hunters)
        {
            hunterAgentGroup.RegisterAgent(hunter);
        }

        foreach (var prey in preys)
        {
            preyAgentGroup.RegisterAgent(prey);
        }
    }

    /// <summary>
    /// Arrête tous les chasseurs en les désactivant temporairement.
    /// </summary>
    void StopHunters()
    {
        huntersReleased = false;
        foreach (var hunter in hunters)
            hunter.GetComponent<HunterAgent>().Deactivate();
    }

    /// <summary>
    /// Active les chasseurs après la période de cachette initiale.
    /// </summary>
    void ReleaseHunters()
    {
        huntersReleased = true;
        foreach (var hunter in hunters)
            hunter.GetComponent<HunterAgent>().Activate();
    }

    /// <summary>
    /// Appelée lorsqu'une proie est attrapée par un chasseur.
    /// </summary>
    /// <param name="prey">La proie attrapée (GameObject)</param>
    public void OnPreyCaught(GameObject prey)
    {
        prey.SetActive(false);
        PreyAgent preyAgent = prey.GetComponent<PreyAgent>();
        preys.Remove(preyAgent);
        disabledPreys.Add(preyAgent);

        if (preys.Count == 0)
        {
            // Toutes les proies sont attrapées → les chasseurs gagnent
            EndEpisode(false);
        }
        else
        {
            // Récompense partielle pour les chasseurs, punition pour les proies
            hunterAgentGroup.AddGroupReward(1);
            preyAgentGroup.AddGroupReward(-1);
        }
    }

    /// <summary>
    /// Termine l'épisode, attribue les récompenses finales et redémarre l'environnement.
    /// </summary>
    /// <param name="timeExpired">Si vrai, l’épisode se termine par dépassement du temps</param>
    void EndEpisode(bool timeExpired)
    {
        if (timeExpired)
        {
            hunterAgentGroup.SetGroupReward(-1);
            preyAgentGroup.SetGroupReward(1);
            preyWins++;
            Debug.Log("Prey Win Count: " + preyWins);
        }
        else
        {
            hunterAgentGroup.SetGroupReward(1);
            preyAgentGroup.SetGroupReward(-1);
            hunterWins++;
            Debug.Log("Hunter Win Count: " + hunterWins);
        }

        hunterAgentGroup.EndGroupEpisode();
        preyAgentGroup.EndGroupEpisode();

        ResetEnv();
    }

    /// <summary>
    /// Réinitialise l'environnement pour un nouvel épisode.
    /// </summary>
    private void ResetEnv()
    {
        timer = 0f;

        // Réactiver les chasseurs désactivés
        foreach (var hunter in disabledHunters)
        {
            hunters.Add(hunter);
            hunter.gameObject.SetActive(true);

            Rigidbody hunterRb = hunter.GetComponent<Rigidbody>();
            if (hunterRb != null)
            {
                hunterRb.linearVelocity = Vector3.zero;
                hunterRb.angularVelocity = Vector3.zero;
                hunterRb.Sleep();
            }
        }
        disabledHunters.Clear();

        // Réactiver les proies désactivées
        foreach (var prey in disabledPreys)
        {
            preys.Add(prey);
            prey.gameObject.SetActive(true);

            Rigidbody preyRb = prey.GetComponent<Rigidbody>();
            if (preyRb != null)
            {
                preyRb.linearVelocity = Vector3.zero;
                preyRb.angularVelocity = Vector3.zero;
                preyRb.Sleep();
            }
        }
        disabledPreys.Clear();

        // Désactive temporairement les chasseurs
        StopHunters();

        Vector3 position;

        // Réinitialiser la position des chasseurs
        foreach (var hunter in hunters)
        {
            position = spawnPoint.position + new Vector3(
                Random.Range(-randomRangeAgent, randomRangeAgent),
                0,
                Random.Range(-randomRangeAgent, randomRangeAgent)
            );
            hunter.transform.position = position;
            hunter.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            Rigidbody hunterRb = hunter.GetComponent<Rigidbody>();
            if (hunterRb != null)
            {
                hunterRb.linearVelocity = Vector3.zero;
                hunterRb.angularVelocity = Vector3.zero;
                hunterRb.Sleep();
            }
        }

        // Réinitialiser la position des proies
        foreach (var prey in preys)
        {
            position = spawnPoint.position + new Vector3(
                Random.Range(-randomRangeAgent, randomRangeAgent),
                0,
                Random.Range(-randomRangeAgent, randomRangeAgent)
            );
            prey.transform.position = position;
            prey.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            Rigidbody preyRb = prey.GetComponent<Rigidbody>();
            if (preyRb != null)
            {
                preyRb.linearVelocity = Vector3.zero;
                preyRb.angularVelocity = Vector3.zero;
                preyRb.Sleep();
            }
        }

        // Relancer le compte à rebours pour libérer les chasseurs
        Invoke(nameof(ReleaseHunters), hidingTime);
    }
}
