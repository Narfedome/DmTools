using DmToolsApp.Data;
using DmToolsApp.Data.Entities;
using DmToolsApp.Models;

namespace DmToolsApp.Services
{
    public class SceneDataService : ISceneDataService
    {
        private readonly AppDatabase _db;

        public SceneDataService(AppDatabase db)
        {
            _db = db;
        }

        // ── Campaigns ──────────────────────────────────────────────

        public async Task<List<Campaign>> GetCampaignsAsync()
        {
            var entities = await _db.Connection.Table<CampaignEntity>().ToListAsync();
            return entities.Select(e => e.ToModel()).ToList();
        }

        public async Task SaveCampaignAsync(Campaign campaign)
        {
            var entity = campaign.ToEntity();
            if (entity.Id == 0)
            {
                await _db.Connection.InsertAsync(entity);
                campaign.Id = entity.Id;
            }
            else
            {
                await _db.Connection.UpdateAsync(entity);
            }
        }

        public async Task DeleteCampaignAsync(Campaign campaign)
        {
            await _db.Connection.DeleteAsync<CampaignEntity>(campaign.Id);
        }

        // ── Sessions (chapitres) ───────────────────────────────────

        public async Task<List<Session>> GetSessionsAsync(int campaignId)
        {
            var entities = await _db.Connection.Table<SessionEntity>()
                .Where(e => e.CampaignId == campaignId)
                .ToListAsync();
            return entities.Select(e => e.ToModel()).ToList();
        }

        public async Task SaveSessionAsync(Session session)
        {
            var entity = session.ToEntity();
            if (entity.Id == 0)
            {
                await _db.Connection.InsertAsync(entity);
                session.Id = entity.Id;
            }
            else
            {
                await _db.Connection.UpdateAsync(entity);
            }
        }

        public async Task DeleteSessionAsync(Session session)
        {
            await _db.Connection.DeleteAsync<SessionEntity>(session.Id);
        }

        // ── Scenes ────────────────────────────────────────────────

        public async Task<List<Scene>> GetScenesAsync(int sessionId)
        {
            var entities = await _db.Connection.Table<SceneEntity>()
                .Where(e => e.SessionId == sessionId)
                .ToListAsync();
            return entities.Select(e => e.ToModel()).ToList();
        }

        public async Task SaveSceneAsync(Scene scene)
        {
            var entity = scene.ToEntity();
            if (entity.Id == 0)
            {
                await _db.Connection.InsertAsync(entity);
                scene.Id = entity.Id;
            }
            else
            {
                await _db.Connection.UpdateAsync(entity);
            }
        }

        public async Task DeleteSceneAsync(Scene scene)
        {
            await _db.Connection.DeleteAsync<SceneEntity>(scene.Id);
        }

        // ── SceneTracks ───────────────────────────────────────────

        public async Task<List<SceneTrack>> GetSceneTracksAsync(int sceneId)
        {
            var sceneTrackEntities = await _db.Connection.Table<SceneTrackEntity>()
                .Where(e => e.SceneId == sceneId)
                .OrderBy(e => e.Position)
                .ToListAsync();

            var result = new List<SceneTrack>();

            foreach (var ste in sceneTrackEntities)
            {
                var trackEntity = await _db.Connection.Table<TrackEntity>()
                    .Where(t => t.Id == ste.TrackId)
                    .FirstOrDefaultAsync();

                if (trackEntity == null) continue;

                // Le volume du strip prime sur le volume par défaut de la track de bibliothèque.
                var track = trackEntity.ToModel();
                track.Volume = ste.Volume;

                result.Add(ste.ToModel(track));
            }

            return result;
        }

        public async Task SaveSceneTrackAsync(SceneTrack sceneTrack)
        {
            var entity = sceneTrack.ToEntity();
            if (entity.Id == 0)
            {
                await _db.Connection.InsertAsync(entity);
                sceneTrack.Id = entity.Id;
            }
            else
            {
                await _db.Connection.UpdateAsync(entity);
            }
        }

        public async Task DeleteSceneTrackAsync(SceneTrack sceneTrack)
        {
            await _db.Connection.DeleteAsync<SceneTrackEntity>(sceneTrack.Id);
        }

        public async Task UpdateSceneTrackAsync(int sceneTrackId, double volume, bool isLooping, bool autoPlay, bool fadeIn, bool fadeOut)
        {
            var entity = await _db.Connection.FindAsync<SceneTrackEntity>(sceneTrackId);
            if (entity == null) return;
            entity.Volume = volume;
            entity.IsLooping = isLooping;
            entity.AutoPlay = autoPlay;
            entity.FadeIn = fadeIn;
            entity.FadeOut = fadeOut;
            await _db.Connection.UpdateAsync(entity);
        }

        public async Task UpdateSceneTrackVolumeAsync(int sceneTrackId, float volume)
        {
            var entity = await _db.Connection.FindAsync<SceneTrackEntity>(sceneTrackId);
            if (entity == null) return;
            entity.Volume = volume;
            await _db.Connection.UpdateAsync(entity);
        }
    }

    public interface ISceneDataService
    {
        Task<List<Campaign>> GetCampaignsAsync();
        Task SaveCampaignAsync(Campaign campaign);
        Task DeleteCampaignAsync(Campaign campaign);

        Task<List<Session>> GetSessionsAsync(int campaignId);
        Task SaveSessionAsync(Session session);
        Task DeleteSessionAsync(Session session);

        Task<List<Scene>> GetScenesAsync(int sessionId);
        Task SaveSceneAsync(Scene scene);
        Task DeleteSceneAsync(Scene scene);

        Task<List<SceneTrack>> GetSceneTracksAsync(int sceneId);
        Task SaveSceneTrackAsync(SceneTrack sceneTrack);
        Task DeleteSceneTrackAsync(SceneTrack sceneTrack);
        Task UpdateSceneTrackAsync(int sceneTrackId, double volume, bool isLooping, bool autoPlay, bool fadeIn, bool fadeOut);
        Task UpdateSceneTrackVolumeAsync(int sceneTrackId, float volume);
    }
}
