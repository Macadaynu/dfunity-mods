using System.Linq;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Game.Macadaynu_Mods
{
    public enum BattleState { START, PLAYERTURN, ENEMYTURN, WON, LOST }

    public class BattleSystem : MonoBehaviour
    {
        public static BattleState state;
        public static int ManaLeft;
        public static TextMeshProUGUI manaLeftText;
        static int ManaPerTurn = 1;
        static int MaxMana = 10;

        void Start()
        {
            state = BattleState.START;
        }

        public static void SetupBattle()
        {
            // Load player cards
            CardLoader.LoadCard("Skeletal Warrior");
            //CardLoader.LoadCard("Vampire Ancient");
            CardLoader.LoadCard("Fire Atronach");
            //CardLoader.LoadCard("Ancient Lich");
            CardLoader.LoadCard("Giant Rat");
            CardLoader.LoadCard("Grizzly Bear");

            // Load Daedric princes
            Minion.SpawnMinion(Board.allCells[2, 0], CardLoader.GetCard("Azura"), true, true);
            Minion.SpawnMinion(Board.allCells[2, 4], CardLoader.GetCard("Hircine"), false, true);

            // Set Mana
            var mana = DaedricCardsMod.boardCanvas.transform.Find("Mana");
            manaLeftText = mana.GetComponent<TextMeshProUGUI>();
            ManaLeft = 0;

            EnemyPlayer.LoadCardHand();
            EnemyPlayer.mana = 0;

            //PlayerTurn();
            EnemyTurn();
        }

        static void PlayerTurn()
        {
            state = BattleState.PLAYERTURN;
            ManaLeft += ManaPerTurn;
            ManaLeft = ManaLeft > MaxMana ? ManaLeft = MaxMana : ManaLeft;
            UpdateManaLeft();
            Board.ApplyCanMoveOrAttackGlow();
        }

        public void EndTurnButton()
        {
            if (state == BattleState.PLAYERTURN)
            {
                EnemyTurn();
            }
        }

        static void EnemyTurn()
        {
            state = BattleState.ENEMYTURN;
            EnemyPlayer.mana += ManaPerTurn;
            EnemyPlayer.mana = EnemyPlayer.mana > MaxMana ? EnemyPlayer.mana = MaxMana : EnemyPlayer.mana;

            //TODO: Move AI Stuff to its own class
            var allMinionsOnBoard = DaedricCardsMod.boardCanvas.transform.GetComponentsInChildren(typeof(Minion), false);
            foreach (Minion minion in allMinionsOnBoard)
            {
                minion.hasMoved = false;
                minion.hasAttacked = false;

                if (!minion.isPlayerMinion && !minion.isDaedricPrince)
                {
                    // check if can attack
                    var validCells = Board.GetValidCellMoves(minion.currentCell).ToArray();
                    foreach (var validCell in validCells)
                    {                        
                        if (validCell.isOccupied)
                        {
                            var defendingMinion = validCell.GetComponentInChildren<Minion>();
                            if (defendingMinion.isPlayerMinion && !defendingMinion.hasDied)
                            {
                                if (AttackCalculator.CalculateTradeWorth(minion, defendingMinion))
                                {
                                    AttackCalculator.AttackMinion(minion, defendingMinion);
                                    break;
                                }
                            }
                        }
                    }

                    // cant move if already attacked
                    if (!minion.hasAttacked)
                    {
                        // check to see where minion can move
                        var validMoves = validCells.Where(x => !x.isOccupied).ToArray();
                        if (validMoves.Any())
                        {
                            //var cellToMoveEnemyMinionTo = validMoves[Random.Range(0, validMoves.Length)];
                            Cell closestCellToPlayerPrince = null;
                            double? closestDistance = null;
                            foreach (var validMove in validMoves)
                            {
                                var distance = Board.GetDistanceBetweenCells(validMove, Board.allCells[2, 0]);
                                if (!closestDistance.HasValue || distance < closestDistance)
                                {
                                    closestDistance = distance;
                                    closestCellToPlayerPrince = validMove;
                                }
                            }

                            closestCellToPlayerPrince.OccupyCell(minion);

                            //var cellsInOrderOfRow = validMoves.OrderBy(x => x.boardPosition.y);
                            //var cellToMoveEnemyMinionTo = cellsInOrderOfRow.First(); // move the minion further down the board

                        }
                    }
                }    
            }

            var nextCardToPlay = EnemyPlayer.cards.FirstOrDefault(x => x.cost <= EnemyPlayer.mana);
            if (nextCardToPlay != null)
            {
                var emptyCells = from Cell cell in Board.allCells
                                 where !cell.isOccupied && cell.boardPosition.y > 2
                                 select cell;

                if (emptyCells.Any())
                {
                    Cell cellToPlay = null;
                    if (!Board.allCells[2, 3].isOccupied)
                    {
                        cellToPlay = Board.allCells[2, 3];
                    }
                    else if (!Board.allCells[0, 4].isOccupied)
                    {
                        cellToPlay = Board.allCells[0, 4];
                    }
                    else if (!Board.allCells[4, 4].isOccupied)
                    {
                        cellToPlay = Board.allCells[4, 4];
                    }
                    else
                    {
                        var emptyCellsArray = emptyCells.ToArray();
                        cellToPlay = emptyCellsArray[Random.Range(0, emptyCellsArray.Length)];
                    }

                    Minion.SpawnMinion(cellToPlay, nextCardToPlay, false);

                    EnemyPlayer.mana -= nextCardToPlay.cost;

                    EnemyPlayer.cards.Remove(nextCardToPlay);
                }
            }

            PlayerTurn();
        }

        public static void UpdateManaLeft(int manaCost = 0)
        {
            ManaLeft -= manaCost;
            manaLeftText.text = ManaLeft.ToString();
        }
    }
}