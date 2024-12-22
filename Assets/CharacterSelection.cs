using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class CharacterSelection : MonoBehaviour
{
    public GameObject[] characters;
    public int selectedCharacter = 0;
    public TMP_Text label;
    public TMP_Text LClick;
    public TMP_Text RClick;
    public TMP_Text OneClick;
    public TMP_Text TwoClick;
    public TMP_Text LClickName;
    public TMP_Text RClickName;
    public TMP_Text OneClickName;
    public TMP_Text TwoClickName;

    public string[] AhriDesc;
    public string[] AsheDesc;
    public string[] CaitlynDesc;

    public string[] AhriNames;
    public string[] AsheNames;
    public string[] CaitlynNames;

    // Variables for dynamic values
    public int ahriLBaseDamage = 5;
    public int ahriQBaseDamage = 20;
    public int ahriWBaseDamage = 40;
    public int ahriEBaseDamage = 50;

    public int asheLBaseDamage = 20;
    public int asheQArrowDamage = 20;
    public int asheQArrowCount = 10;
    public int asheEArrowDamage = 20;
    public int asheEArrowCount = 20;
    public int asheRBaseDamage = 200;
    public int asheRAoeDamage = 50;

    public int caitlynLBaseDamage = 50;
    public int caitlynRBaseDamage = 20;
    public int caitlynQBaseDamage = 50;
    public int caitlynEBaseDamage = 40;
    public int piercing = 2;

    private void Start() {
        AhriNames = new string[] {
            $"Spirit Shot",
            $"Enchanted Allure",
            $"Flickering Flames",
            $"Twin Echo"
        };

        AsheNames = new string[] {
            $"Frost Arrow",
            $"Rapid Barrage",
            $"Hailstorm Arrows",
            $"Glacial Strike"
        };
        
        CaitlynNames = new string[] {
            $"Precision Shot",
            $"Sniper's Mark",
            $"Explosive Trap",
            $"Net Escape"
        };


        // Generate descriptions dynamically
        AhriDesc = new string[] {
            $"Fires a single energy orb forward, dealing damage to the first target it hits ({ahriLBaseDamage}).",
            $"Fires a heart-shaped projectile that damages and charms the first enemy hit, forcing them to slowly walk toward Ahri for a short duration. ({ahriEBaseDamage})",
            $"Summons three flames that orbit Ahri, targeting and firing at nearby enemies. Each flame deals damage upon impact. ({ahriWBaseDamage})",
            $"Launches an orb forward that damages enemies it passes through and returns to Ahri after reaching its maximum distance. The returning orb also deals damage to enemies in its path. ({ahriQBaseDamage})"
        };

        AsheDesc = new string[] {
            $"Fires a single arrow straight forward, dealing damage to the first target it hits. ({asheLBaseDamage})",
            $"Rapidly fires multiple arrows with a slight spread over a short time, dealing damage to enemies in quick succession. ({asheEArrowCount}*{asheEArrowDamage})",
            $"Fires a spread of arrows in a cone-shaped pattern. Each arrow deals damage to enemies in its path. ({asheQArrowCount}*{asheQArrowDamage})",
            $"Fires a large arrow that deals heavy damage upon impact and creates an area effect that deals decreasing damage based on distance. ({asheRBaseDamage}; {asheRAoeDamage} AOE)"
        };

        CaitlynDesc = new string[] {
            $"Fires a precise bullet forward, dealing significant damage to enemies in its path. The bullet can pierce through up to {piercing} enemy targets ({caitlynLBaseDamage}).",
            $"Marks an enemy with a stun and a knockback effect. The shot deals minor damage but focuses on controlling the enemy ({caitlynRBaseDamage}).",
            $"Places a trap that stuns and traps enemies upon activation. The trap is immovable and lasts for a limited duration ({caitlynQBaseDamage}).",
            $"Fires a net as a projectile that deals solid damage, stuns the first enemy it hits, and pushes Caitlyn backward for repositioning ({caitlynEBaseDamage})."
        };

        label.text = characters[selectedCharacter].name;
        LClick.text = AhriDesc[0];
        LClickName.text = AhriNames[0];
        RClick.text =  AhriDesc[1];
        RClickName.text = AhriNames[1];
        OneClick.text = AhriDesc[2];
        OneClickName.text = AhriNames[2];
        TwoClick.text = AhriDesc[3];
        TwoClickName.text = AhriNames[3];
    }

    public void NextCharacter()
    {
        characters[selectedCharacter].SetActive(false);
        selectedCharacter = (selectedCharacter + 1) % characters.Length;
        characters[selectedCharacter].SetActive(true);
        label.text = characters[selectedCharacter].name;
    }

    public void PreviousCharacter()
    {
        characters[selectedCharacter].SetActive(false);
        selectedCharacter--;
        if(selectedCharacter < 0)
        {
            selectedCharacter += characters.Length;
        }
        characters[selectedCharacter].SetActive(true);
        label.text = characters[selectedCharacter].name;
    }

    public void Ahri()
    {
        characters[selectedCharacter].SetActive(false);
        selectedCharacter = 0;
        characters[selectedCharacter].SetActive(true);
        label.text = characters[selectedCharacter].name;
        LClick.text = AhriDesc[0];
        LClickName.text = AhriNames[0];
        RClick.text =  AhriDesc[1];
        RClickName.text = AhriNames[1];
        OneClick.text = AhriDesc[2];
        OneClickName.text = AhriNames[2];
        TwoClick.text = AhriDesc[3];
        TwoClickName.text = AhriNames[3];
    }

    public void Ashe()
    {
        characters[selectedCharacter].SetActive(false);
        selectedCharacter = 1;
        characters[selectedCharacter].SetActive(true);
        label.text = characters[selectedCharacter].name;
        LClick.text = AsheDesc[0];
        LClickName.text = AsheNames[0];
        RClick.text =  AsheDesc[1];
        RClickName.text = AsheNames[1];
        OneClick.text = AsheDesc[2];
        OneClickName.text = AsheNames[2];
        TwoClick.text = AsheDesc[3];
        TwoClickName.text = AsheNames[3];
    }

    public void Cait()
    {
        characters[selectedCharacter].SetActive(false);
        selectedCharacter = 2;
        characters[selectedCharacter].SetActive(true);
        label.text = characters[selectedCharacter].name;
        LClick.text = CaitlynDesc[0];
        LClickName.text = CaitlynNames[0];
        RClick.text =  CaitlynDesc[1];
        RClickName.text = CaitlynNames[1];
        OneClick.text = CaitlynDesc[2];
        OneClickName.text = CaitlynNames[2];
        TwoClick.text = CaitlynDesc[3];
        TwoClickName.text = CaitlynNames[3];
    }

    public void Galio()
    {
        characters[selectedCharacter].SetActive(false);
        selectedCharacter = 3;
        characters[selectedCharacter].SetActive(true);
        label.text = characters[selectedCharacter].name;
    }

    public void StartGame()
    {
        PlayerPrefs.SetInt("selectedCharacter", selectedCharacter);
        SceneManager.LoadScene(1, LoadSceneMode.Single);
    }
}
