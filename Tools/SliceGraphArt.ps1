Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$graph = Join-Path $root "Graph"
$resources = Join-Path $root "Assets/Resources/Art"
$sourceSheets = Join-Path $root "Assets/Art/SourceSheets"

New-Item -ItemType Directory -Force -Path $resources, $sourceSheets | Out-Null
Get-ChildItem -Path $graph -Recurse -Filter "*.png" | Copy-Item -Destination $sourceSheets -Force

function Save-Cell {
    param(
        [Parameter(Mandatory)] [string] $Sheet,
        [Parameter(Mandatory)] [int] $Columns,
        [Parameter(Mandatory)] [int] $Rows,
        [Parameter(Mandatory)] [string[]] $Names,
        [Parameter(Mandatory)] [string] $OutputFolder,
        [bool] $TransparentWhite = $true
    )

    $sourcePath = Join-Path $graph $Sheet
    if (!(Test-Path $sourcePath)) {
        $sourcePath = Join-Path (Join-Path $graph "st 2") $Sheet
    }

    if (!(Test-Path $sourcePath)) {
        throw "Sheet not found: $Sheet"
    }

    $outDir = Join-Path $resources $OutputFolder
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null

    $image = [System.Drawing.Bitmap]::FromFile($sourcePath)
    try {
        $cellWidth = [int]($image.Width / $Columns)
        $cellHeight = [int]($image.Height / $Rows)

        for ($i = 0; $i -lt $Names.Count; $i++) {
            if ([string]::IsNullOrWhiteSpace($Names[$i])) {
                continue
            }

            $x = ($i % $Columns) * $cellWidth
            $y = [Math]::Floor($i / $Columns) * $cellHeight
            if ($y -ge $image.Height) {
                break
            }

            $rect = New-Object System.Drawing.Rectangle($x, $y, $cellWidth, $cellHeight)
            $cell = New-Object System.Drawing.Bitmap($cellWidth, $cellHeight, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            $graphics = [System.Drawing.Graphics]::FromImage($cell)
            try {
                $graphics.DrawImage($image, 0, 0, $rect, [System.Drawing.GraphicsUnit]::Pixel)
            }
            finally {
                $graphics.Dispose()
            }
            try {
                if ($TransparentWhite) {
                    for ($py = 0; $py -lt $cell.Height; $py++) {
                        for ($px = 0; $px -lt $cell.Width; $px++) {
                            $color = $cell.GetPixel($px, $py)
                            $min = [Math]::Min($color.R, [Math]::Min($color.G, $color.B))
                            if ($min -ge 248) {
                                $cell.SetPixel($px, $py, [System.Drawing.Color]::FromArgb(0, $color.R, $color.G, $color.B))
                            } elseif ($min -ge 238) {
                                $alpha = [Math]::Max(0, [Math]::Min(255, (248 - $min) * 25))
                                $cell.SetPixel($px, $py, [System.Drawing.Color]::FromArgb($alpha, $color.R, $color.G, $color.B))
                            }
                        }
                    }

                    $minX = $cell.Width
                    $minY = $cell.Height
                    $maxX = -1
                    $maxY = -1
                    for ($py = 0; $py -lt $cell.Height; $py++) {
                        for ($px = 0; $px -lt $cell.Width; $px++) {
                            if ($cell.GetPixel($px, $py).A -gt 8) {
                                $minX = [Math]::Min($minX, $px)
                                $minY = [Math]::Min($minY, $py)
                                $maxX = [Math]::Max($maxX, $px)
                                $maxY = [Math]::Max($maxY, $py)
                            }
                        }
                    }

                    if ($maxX -ge 0 -and $maxY -ge 0) {
                        $padding = 10
                        $minX = [Math]::Max(0, $minX - $padding)
                        $minY = [Math]::Max(0, $minY - $padding)
                        $maxX = [Math]::Min($cell.Width - 1, $maxX + $padding)
                        $maxY = [Math]::Min($cell.Height - 1, $maxY + $padding)
                        $trimRect = New-Object System.Drawing.Rectangle($minX, $minY, ($maxX - $minX + 1), ($maxY - $minY + 1))
                        $trimmed = $cell.Clone($trimRect, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
                        $cell.Dispose()
                        $cell = $trimmed
                    }
                }

                $outPath = Join-Path $outDir ($Names[$i] + ".png")
                $cell.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
            }
            finally {
                $cell.Dispose()
            }
        }
    }
    finally {
        $image.Dispose()
    }
}

Save-Cell "bg_login_map_village_sheet_white_bg.png" 3 1 @(
    "bg_login_adventure_gate",
    "bg_world_map_parchment",
    "bg_home_village_day"
) "Backgrounds" $false

Save-Cell "bg_guild_forest_cave_sheet_white_bg.png" 3 1 @(
    "bg_guild_hall_interior",
    "bg_battle_sunny_forest",
    "bg_battle_crystal_cave"
) "Backgrounds" $false

Save-Cell "hero_sprite_sheet_white_bg.png" 3 2 @(
    "char_child_hero_idle",
    "char_child_hero_attack",
    "char_child_hero_hurt",
    "char_child_hero_victory",
    "char_child_hero_portrait"
) "Characters/Hero"

Save-Cell "enemy_slime_bat_sheet_white_bg.png" 4 2 @(
    "enemy_slime_green_idle",
    "enemy_slime_green_attack",
    "enemy_slime_green_hurt",
    "enemy_slime_green_defeat",
    "enemy_cave_bat_idle",
    "enemy_cave_bat_attack",
    "enemy_cave_bat_hurt",
    "enemy_cave_bat_defeat"
) "Characters/Enemies"

Save-Cell "enemy_golem_bonus_sheet_white_bg.png" 4 2 @(
    "enemy_stone_golem_idle",
    "enemy_stone_golem_attack",
    "enemy_stone_golem_hurt",
    "enemy_stone_golem_defeat",
    "enemy_forest_mushroom",
    "enemy_leaf_imp",
    "enemy_crystal_spider"
) "Characters/Enemies"

Save-Cell "enemy_sheet_crab_sand_white_bg.png" 4 3 @(
    "enemy_sand_crab_idle",
    "enemy_sand_crab_attack",
    "enemy_sand_crab_hurt",
    "enemy_sand_crab_defeat",
    "enemy_sand_spirit_idle",
    "enemy_sand_spirit_attack",
    "enemy_sand_spirit_hurt",
    "enemy_sand_spirit_defeat",
    "enemy_shell_guardian_idle",
    "enemy_shell_guardian_attack",
    "enemy_shell_guardian_hurt",
    "enemy_shell_guardian_defeat"
) "Characters/Enemies"

Save-Cell "enemy_sheet_slime_mosquito_white_bg.png" 4 3 @(
    "enemy_blue_slime_idle",
    "enemy_blue_slime_attack",
    "enemy_blue_slime_hurt",
    "enemy_blue_slime_defeat",
    "enemy_mosquito_idle",
    "enemy_mosquito_attack",
    "enemy_mosquito_hurt",
    "enemy_mosquito_defeat",
    "enemy_poison_slime_idle",
    "enemy_poison_slime_attack",
    "enemy_poison_slime_hurt",
    "enemy_poison_slime_defeat"
) "Characters/Enemies"

Save-Cell "items_loot_sheet_white_bg.png" 3 3 @(
    "item_wooden_sword",
    "item_shiny_pebble",
    "item_training_shield",
    "item_leaf_cloak",
    "item_crystal_ring",
    "item_health_potion_small",
    "item_gold_coin_stack",
    "item_xp_star",
    "item_reward_chest"
) "Items"

Save-Cell "equipment_icons_sheet_white_bg.png" 4 3 @(
    "item_iron_sword",
    "item_forest_bow",
    "item_apprentice_staff",
    "item_wooden_shield",
    "item_leather_armor",
    "item_mage_hat",
    "item_swift_boots",
    "item_lucky_amulet",
    "item_crystal_dagger",
    "item_guardian_helmet",
    "item_sun_badge",
    "item_moon_charm"
) "Items"

Save-Cell "materials_potions_sheet_white_bg.png" 4 3 @(
    "material_wood",
    "material_stone",
    "material_leaf",
    "material_crystal",
    "potion_health_small",
    "potion_energy_small",
    "potion_focus_small",
    "potion_luck_small",
    "material_mushroom",
    "material_feather",
    "material_shell",
    "material_spark"
) "Items"

Save-Cell "ui_icons_sheet_white_bg.png" 3 3 @(
    "ui_icon_attack",
    "ui_icon_defend",
    "ui_icon_bag",
    "ui_icon_map",
    "ui_icon_guild",
    "ui_icon_home",
    "ui_icon_settings",
    "ui_icon_back",
    "ui_icon_refresh"
) "UI/Icons"

Save-Cell "ui_panels_buttons_sheet_white_bg.png" 4 3 @(
    "ui_panel_parchment",
    "ui_panel_dark_wood",
    "ui_panel_quest_card",
    "ui_button_primary",
    "ui_button_secondary",
    "ui_button_success",
    "ui_button_danger",
    "ui_input_field",
    "ui_badge_assigned",
    "ui_badge_submitted",
    "ui_badge_approved",
    "ui_badge_rejected"
) "UI/Panels"

Save-Cell "map_town_assets_sheet_white_bg.png" 4 3 @(
    "map_node_home_village",
    "map_node_sunny_forest",
    "map_node_crystal_cave",
    "map_node_locked",
    "map_node_selected_ring",
    "map_path_dirt",
    "map_path_dotted",
    "map_compass_icon",
    "town_building_guild",
    "town_notice_board",
    "town_reward_chest",
    "town_training_dummy"
) "Map"

Save-Cell "map_nodes_paths_sheet_white_bg.png" 4 3 @(
    "map_node_castle",
    "map_node_tower",
    "map_node_swamp",
    "map_node_mountain",
    "map_path_stone",
    "map_path_bridge",
    "map_lock_chain",
    "map_star_marker",
    "map_cloud_unexplored",
    "map_flag_blue",
    "map_flag_gold",
    "map_boss_marker"
) "Map"

Save-Cell "town_buildings_props_sheet_white_bg.png" 4 3 @(
    "town_building_shop",
    "town_building_blacksmith",
    "town_building_home",
    "town_building_alchemist",
    "town_lantern",
    "town_signpost",
    "town_fountain",
    "town_market_stall",
    "town_barrel",
    "town_crate",
    "town_flower_pot",
    "town_well"
) "Town"

Save-Cell "quests_fx_npc_sheet_white_bg.png" 4 3 @(
    "quest_icon_scroll",
    "quest_stamp_submitted",
    "quest_stamp_approved",
    "quest_stamp_rejected",
    "fx_hit_slash",
    "fx_hit_spark",
    "fx_reward_claim_glow",
    "fx_level_up_burst",
    "portrait_guild_master",
    "portrait_village_elder",
    "portrait_shopkeeper",
    "portrait_blacksmith"
) "Quests"

Save-Cell "real_life_quest_icons_sheet_white_bg.png" 4 3 @(
    "quest_icon_reading",
    "quest_icon_study",
    "quest_icon_home",
    "quest_icon_health",
    "quest_icon_sport",
    "quest_icon_hygiene",
    "quest_icon_family_help",
    "quest_icon_custom",
    "quest_board_empty",
    "quest_board_has_tasks",
    "quest_icon_star_reward",
    "quest_icon_parent_check"
) "Quests"

Save-Cell "effects_rewards_sheet_white_bg.png" 4 3 @(
    "fx_heal_green",
    "fx_gold_burst",
    "fx_xp_sparkle",
    "fx_button_press_flash",
    "fx_enemy_defeat_smoke",
    "fx_quest_submit_glow",
    "fx_magic_blue",
    "fx_fire_small",
    "fx_shield_pulse",
    "fx_crit_burst",
    "fx_coin_single",
    "fx_treasure_open"
) "FX"

Save-Cell "npc_portraits_sheet_white_bg.png" 4 3 @(
    "portrait_child_hero_default",
    "portrait_parent_default",
    "portrait_guild_master_alt",
    "portrait_village_elder_alt",
    "portrait_shopkeeper_alt",
    "portrait_blacksmith_alt",
    "portrait_alchemist",
    "portrait_trainer",
    "portrait_merchant",
    "portrait_guard",
    "portrait_messenger",
    "portrait_mysterious_friend"
) "Characters/Portraits"

Save-Cell "rarity_frames_badges_sheet_white_bg.png" 4 3 @(
    "rarity_frame_common",
    "rarity_frame_uncommon",
    "rarity_frame_rare",
    "rarity_frame_epic",
    "rarity_frame_legendary",
    "ui_badge_claimed",
    "ui_badge_cancelled",
    "ui_badge_locked",
    "ui_frame_gold",
    "ui_frame_silver",
    "ui_frame_bronze",
    "ui_panel_slot"
) "UI/Panels"

Write-Host "Graph art sliced into $resources"
