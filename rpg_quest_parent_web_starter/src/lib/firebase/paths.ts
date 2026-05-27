export const paths = {
  user: (uid: string) => `users/${uid}`,
  family: (familyId: string) => `families/${familyId}`,
  child: (childId: string) => `children/${childId}`,
  childGameState: (childId: string) => `children/${childId}/gameState/main`,
  childQuests: (childId: string) => `children/${childId}/quests`,
  childQuest: (childId: string, questId: string) => `children/${childId}/quests/${questId}`,
  childAnalyticsDaily: (childId: string) => `children/${childId}/analyticsDaily`,
};
