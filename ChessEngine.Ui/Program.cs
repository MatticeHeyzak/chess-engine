using ChessEngine.Core.BoardState;
using ChessEngine.Ui;
using ChessEngine.Ui.Renderer;
using ChessEngine.Ui.Updates;

var fen = args.Length > 0 ? args[0] : Fen.StartPosition;
var session = new GameSession(fen);

const int botThinkTimeMs = 1000;
var layout = new BoardLayout(originX: 24, originY: 24, squareSize: 100);
var interaction = new BoardInteraction(session, layout);

IRenderer[] renderers =
[
    new BoardRenderer(layout),
    new LastMoveRenderer(layout, session),
    new CheckRenderer(layout, session),
    new MoveHighlightRenderer(layout, interaction),
    new PieceRenderer(session, layout, interaction),
    new PromotionPickerRenderer(session, layout, interaction),
    new SidebarRenderer(session, layout),
    new GameOverRenderer(session),
    new MenuRenderer(session)
];

var bot = new BotController(session, botThinkTimeMs);
new ChessApplication(renderers, [interaction, bot], layout).Run();
