using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared.Administration.Notes;
using Content.Shared.Eui;
using static Content.Shared.Administration.Notes.AdminNoteEuiMsg;

namespace Content.Server.Administration.Notes;

public sealed class AdminNotesEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IAdminNotesManager _notesMan = default!;

    public AdminNotesEui()
    {
        IoCManager.InjectDependencies(this);
    }

    private Guid NotedPlayer { get; set; }
    private string NotedPlayerName { get; set; } = string.Empty;
    private Dictionary<int, SharedAdminNote> Notes { get; set; } = new();

    public override async void Opened()
    {
        base.Opened();

        _admins.OnPermsChanged += OnPermsChanged;
        _notesMan.NoteAdded += NoteModified;
        _notesMan.NoteModified += NoteModified;
        _notesMan.NoteDeleted += NoteDeleted;
    }

    public override void Closed()
    {
        base.Closed();

        _admins.OnPermsChanged -= OnPermsChanged;
        _notesMan.NoteAdded -= NoteModified;
        _notesMan.NoteModified -= NoteModified;
        _notesMan.NoteDeleted -= NoteDeleted;
    }

    public override EuiStateBase GetNewState()
    {
        return new AdminNotesEuiState(
            NotedPlayerName,
            Notes,
            _notesMan.CanCreate(Player),
            _notesMan.CanDelete(Player),
            _notesMan.CanEdit(Player)
        );
    }

    public override async void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        switch (msg)
        {
            case Close _:
            {
                Close();
                break;
            }
            case CreateNoteRequest request:
            {
                if (!_notesMan.CanCreate(Player))
                {
                    Close();
                    break;
                }

                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    break;
                }

                if (request.ExpiryTime is not null && request.ExpiryTime <= DateTime.UtcNow)
                {
                    break;
                }

                await _notesMan.AddNote(Player, NotedPlayer, request.NoteType, request.Message, request.NoteSeverity, request.Secret, request.ExpiryTime);
                break;
            }
            case DeleteNoteRequest request:
            {
                if (!_notesMan.CanDelete(Player))
                {
                    Close();
                    break;
                }

                await _notesMan.DeleteNote(request.Id, Player);
                break;
            }
            case EditNoteRequest request:
            {
                if (!_notesMan.CanEdit(Player))
                {
                    Close();
                    break;
                }

                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    break;
                }

                await _notesMan.ModifyNote(request.Id, Player, request.Message, request.NoteSeverity, request.Secret, request.ExpiryTime);
                break;
            }
        }
    }

    public async Task ChangeNotedPlayer(Guid notedPlayer)
    {
        NotedPlayer = notedPlayer;
        await LoadFromDb();
    }

    private void NoteModified(SharedAdminNote note)
    {
        Notes[note.Id] = note;
        StateDirty();
    }

    private void NoteDeleted(int id)
    {
        Notes.Remove(id);
        StateDirty();
    }

    private async Task LoadFromDb()
    {
        NotedPlayerName = await _notesMan.GetPlayerName(NotedPlayer);

        var notes = new Dictionary<int, SharedAdminNote>();
        foreach (var note in await _notesMan.GetAllNotes(NotedPlayer))
        {
            notes.Add(note.Id, note.ToShared());
        }

        Notes = notes;

        StateDirty();
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !_notesMan.CanView(Player))
        {
            Close();
        }
        else
        {
            StateDirty();
        }
    }
}
