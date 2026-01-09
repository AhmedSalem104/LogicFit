-- Fix Foods Identity Issue
-- This script will hard delete all foods and reset the identity to start from 1
-- Run this ONCE to fix the database, then restart the API to reseed

-- Step 1: Check for any MealItems referencing foods (preview)
SELECT COUNT(*) AS MealItemsCount FROM MealItems WHERE IsDeleted = 0;
SELECT COUNT(*) AS RecipeIngredientsCount FROM RecipeIngredients WHERE IsDeleted = 0;

-- Step 2: Delete MealItems first (if any - they reference foods)
DELETE FROM MealItems;

-- Step 3: Delete RecipeIngredients (if any)
DELETE FROM RecipeIngredients;

-- Step 4: Delete FoodMicronutrients (if any)
DELETE FROM FoodMicronutrients;

-- Step 5: Hard delete ALL foods (including soft-deleted ones)
DELETE FROM Foods;

-- Step 6: Reset the identity seed to 1
DBCC CHECKIDENT ('Foods', RESEED, 0);

-- Step 7: Verify
SELECT IDENT_CURRENT('Foods') AS CurrentIdentity;

PRINT 'Foods table cleaned up. Restart the API to reseed with IDs starting from 1.';
