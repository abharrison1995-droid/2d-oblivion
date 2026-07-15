Portrait images — drop-in format.

Format: PNG, square, 512x512 (any size works, but square avoids stretching —
UiFactory.Portrait sets preserveAspect, so non-square just letterboxes).

Name the file exactly as the portraitId used in code, no subfolders:
  hero_diplomats.png
  hero_traders.png
  hero_nomads.png
  hero_healers.png
  lord_void.png

Texture import settings (Sprite mode, alpha transparency, no mipmaps) are set
automatically on import by Assets/_Project/Editor/PortraitImportSettings.cs —
just drag the file in, no Inspector steps needed.

Until a real file exists for a given id, UiFactory.Portrait shows a colored
placeholder square with initials instead of failing or leaving a blank gap —
so art can land at any time without a code change.

Current portraitIds in use:
  hero_diplomats / hero_traders / hero_nomads / hero_healers
    — player portrait in CharacterCreationUI, keyed off the chosen family
      (Assets/_Project/Scripts/UI/CharacterCreationUI.cs)
  lord_void
    — Lord Void's portrait in the Act 1 audience scene
      (Assets/_Project/Scripts/UI/VoidAudienceUI.cs)
