using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Map
{
    public class MapPlayerTracker : MonoBehaviour
    {
        public bool lockAfterSelecting = false;
        public float enterNodeDelay = 1f;
        public MapManager mapManager;
        public MapView view;

        public static MapPlayerTracker Instance;

        [SerializeField] private EnemyMapManager enemyList;

        public bool Locked { get; set; }

        private void Awake()
        {
            Instance = this;
        }

        public void SelectNode(MapNode mapNode)
        {
            if (Locked) return;

            // Debug.Log("Selected node: " + mapNode.Node.point);

            if (mapManager.CurrentMap.path.Count == 0)
            {
                // player has not selected the node yet, he can select any of the nodes with y = 0
                if (mapNode.Node.point.y == 0)
                    SendPlayerToNode(mapNode);
                else
                    PlayWarningThatNodeCannotBeAccessed();
            }
            else
            {
                Vector2Int currentPoint = mapManager.CurrentMap.path[mapManager.CurrentMap.path.Count - 1];
                Node currentNode = mapManager.CurrentMap.GetNode(currentPoint);

                if (currentNode != null && currentNode.outgoing.Any(point => point.Equals(mapNode.Node.point)))
                    SendPlayerToNode(mapNode);
                else
                    PlayWarningThatNodeCannotBeAccessed();
            }
        }

        private void SendPlayerToNode(MapNode mapNode)
        {
            Locked = lockAfterSelecting;
            mapManager.CurrentMap.path.Add(mapNode.Node.point);
            mapManager.SaveMap();
            view.SetAttainableNodes();
            view.SetLineColors();
            mapNode.ShowSwirlAnimation();
            GameSession.node = mapNode.Blueprint;
            switch (mapNode.Node.nodeType)
            {
                case NodeType.MinorEnemy:
                    SetMinorEnemy();
                    break;
                case NodeType.EliteEnemy:
                    SetEliteEnemy();
                    break;
                case NodeType.Boss:
                    SetBossEnemy();
                    break;
                default:
                    break;
            }
            DOTween.Sequence().AppendInterval(enterNodeDelay).OnComplete(() => EnterNode(mapNode));
        }

        private static void EnterNode(MapNode mapNode)
        {
            // we have access to blueprint name here as well
            Debug.Log("Entering node: " + mapNode.Node.blueprintName + " of type: " + mapNode.Node.nodeType);
            // load appropriate scene with context based on nodeType:
            // or show appropriate GUI over the map: 
            // if you choose to show GUI in some of these cases, do not forget to set "Locked" in MapPlayerTracker back to false
            
            switch (mapNode.Node.nodeType)
            {
                case NodeType.MinorEnemy:
                    // open card game
                    Debug.Log("Open card game");
                    SceneManager.LoadScene(3);
                    break;
                case NodeType.EliteEnemy:
                    // open card game
                    Debug.Log("Open card game");
                    SceneManager.LoadScene(3);
                    break;
                case NodeType.RestSite:
                    // open rest site GUI
                    GameSession.heroes.HealForRest();
                    var ManagerMap = FindObjectOfType<HeroCardMapManager>();
                    ManagerMap.HealHero(); 
                    break;
                case NodeType.Treasure:
                    break;
                case NodeType.Store:
                    break;
                case NodeType.Boss:
                    // open card game
                    Debug.Log("Open card game");
                    SceneManager.LoadScene(3);
                    break;
                case NodeType.Mystery:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private void PlayWarningThatNodeCannotBeAccessed()
        {
            Debug.Log("Selected node cannot be accessed");
        }
        public void SetMinorEnemy()
        {
            // Get the random minor enemy scriptable object
            EnemyCardScriptable enemyScriptable = enemyList.GetRandomMinorEnemy();

            // Assign the scriptable object directly to the node blueprint
            if (GameSession.node != null)
            {
                GameSession.node.enemyCharacter = enemyScriptable;
                GameSession.enemies = new EnemyCardData(enemyScriptable);
            }
        }
        public void SetEliteEnemy()
        {
            // Get the random minor enemy scriptable object
            EnemyCardScriptable enemyScriptable = enemyList.GetRandomEliteEnemy();

            // Assign the scriptable object directly to the node blueprint
            if (GameSession.node != null)
            {
                GameSession.node.enemyCharacter = enemyScriptable;
                GameSession.enemies = new EnemyCardData(enemyScriptable);
            }
        }
        public void SetBossEnemy()
        {
            // Get the random minor enemy scriptable object
            EnemyCardScriptable enemyScriptable = enemyList.GetRandomBoss();

            // Assign the scriptable object directly to the node blueprint
            if (GameSession.node != null)
            {
                GameSession.node.enemyCharacter = enemyScriptable;
                GameSession.enemies = new EnemyCardData(enemyScriptable);
            }
        }
    }
}