import { useEffect, useState } from "react";
import { api, download, errorMessage } from "../api";
import { ErrorBox, InfoBox, Loading, PrototypeTag } from "../components";
import { useNotImplemented, useToast } from "../context";
import { DAYS, hour } from "../format";
import type { AvailabilitySlot, Profile, Skill } from "../types";

interface SkillRow {
  skillId: number;
  level: number;
}

/** Dalībnieka profils (prasības 3.1.–3.4.) un personas datu pārvaldība (9.1., 9.2., 9.4.). */
export default function ProfilePage() {
  const toast = useToast();
  const notImplemented = useNotImplemented();

  const [catalog, setCatalog] = useState<Skill[]>([]);
  const [roles, setRoles] = useState<string[]>([]);
  const [profile, setProfile] = useState<Profile | null>(null);

  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [preferredRole, setPreferredRole] = useState("");
  const [interests, setInterests] = useState("");
  const [skills, setSkills] = useState<SkillRow[]>([]);
  const [availability, setAvailability] = useState<AvailabilitySlot[]>([]);

  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [savedAt, setSavedAt] = useState<Date | null>(null);

  const fill = (p: Profile) => {
    setProfile(p);
    setFirstName(p.firstName);
    setLastName(p.lastName);
    setPreferredRole(p.preferredRole ?? "");
    setInterests(p.interests.join(", "));
    setSkills(p.skills.map((s) => ({ skillId: s.skillId, level: s.level })));
    setAvailability(p.availability);
  };

  useEffect(() => {
    Promise.all([api<Skill[]>("/skills"), api<string[]>("/skills/roles"), api<Profile>("/profile")])
      .then(([c, r, p]) => {
        setCatalog(c);
        setRoles(r);
        fill(p);
      })
      .catch((e) => setError(errorMessage(e)));
  }, []);

  const save = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);
    try {
      const updated = await api<Profile>("/profile", {
        method: "PUT",
        body: {
          firstName,
          lastName,
          preferredRole: preferredRole || null,
          interests: interests.split(",").map((s) => s.trim()).filter(Boolean),
          skills,
          availability,
        },
      });
      fill(updated);
      setSavedAt(new Date());
      toast("Profils saglabāts datubāzē.");
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setSaving(false);
    }
  };

  if (!profile) return error ? <ErrorBox message={error} /> : <Loading />;

  const usedSkillIds = new Set(skills.map((s) => s.skillId));
  const firstFreeSkill = catalog.find((c) => !usedSkillIds.has(c.id));
  const categories = [...new Set(catalog.map((c) => c.category))];

  return (
    <>
      <div className="page-head">
        <h1>Mans profils</h1>
      </div>
      <InfoBox>
        Komandu saskaņošana balstās tikai uz profila datiem – prasmēm un to līmeņiem, pieejamo laiku un vēlamo lomu.
      </InfoBox>

      <form onSubmit={save} className="profile-grid">
        <section className="card">
          <h2>Pamatdati</h2>
          <div className="row">
            <label>
              Vārds
              <input value={firstName} onChange={(e) => setFirstName(e.target.value)} />
            </label>
            <label>
              Uzvārds
              <input value={lastName} onChange={(e) => setLastName(e.target.value)} />
            </label>
          </div>
          <label>
            E-pasts
            <input value={profile.email} disabled />
          </label>
          <label>
            Vēlamā loma komandā
            <select value={preferredRole} onChange={(e) => setPreferredRole(e.target.value)}>
              <option value="">— izvēlies —</option>
              {roles.map((r) => (
                <option key={r}>{r}</option>
              ))}
            </select>
          </label>
          <label>
            Intereses (atdalītas ar komatu)
            <input
              value={interests}
              onChange={(e) => setInterests(e.target.value)}
              placeholder="piem., tīmekļa izstrāde, spēles"
            />
          </label>
        </section>

        <section className="card">
          <h2>Prasmes un līmenis (1–5)</h2>
          {skills.length === 0 && <p className="muted">Nav pievienota neviena prasme.</p>}
          {skills.map((row, i) => (
            <div className="row row-tight" key={i}>
              <select
                aria-label="Prasme"
                value={row.skillId}
                onChange={(e) =>
                  setSkills(skills.map((s, j) => (j === i ? { ...s, skillId: Number(e.target.value) } : s)))
                }
              >
                {categories.map((cat) => (
                  <optgroup key={cat} label={cat}>
                    {catalog
                      .filter((c) => c.category === cat && (c.id === row.skillId || !usedSkillIds.has(c.id)))
                      .map((c) => (
                        <option key={c.id} value={c.id}>
                          {c.name}
                        </option>
                      ))}
                  </optgroup>
                ))}
              </select>
              <select
                aria-label="Līmenis"
                className="select-small"
                value={row.level}
                onChange={(e) =>
                  setSkills(skills.map((s, j) => (j === i ? { ...s, level: Number(e.target.value) } : s)))
                }
              >
                {[1, 2, 3, 4, 5].map((l) => (
                  <option key={l} value={l}>
                    {l}
                  </option>
                ))}
              </select>
              <button
                type="button"
                className="btn btn-small btn-ghost"
                aria-label="Noņemt prasmi"
                onClick={() => setSkills(skills.filter((_, j) => j !== i))}
              >
                ✕
              </button>
            </div>
          ))}
          <button
            type="button"
            className="btn btn-small"
            disabled={!firstFreeSkill}
            onClick={() => firstFreeSkill && setSkills([...skills, { skillId: firstFreeSkill.id, level: 3 }])}
          >
            + Pievienot prasmi
          </button>
          <p className="muted small">1 – pamatzināšanas, 3 – patstāvīgi strādā, 5 – eksperts.</p>
        </section>

        <section className="card">
          <h2>Pieejamais laiks</h2>
          {availability.length === 0 && <p className="muted">Nav norādīts pieejamais laiks.</p>}
          {availability.map((slot, i) => (
            <div className="row row-tight" key={i}>
              <select
                aria-label="Diena"
                value={slot.dayOfWeek}
                onChange={(e) =>
                  setAvailability(
                    availability.map((s, j) => (j === i ? { ...s, dayOfWeek: Number(e.target.value) } : s)),
                  )
                }
              >
                {DAYS.map((d, idx) => (
                  <option key={d} value={idx + 1}>
                    {d}
                  </option>
                ))}
              </select>
              <select
                aria-label="No"
                className="select-small"
                value={slot.startHour}
                onChange={(e) =>
                  setAvailability(
                    availability.map((s, j) => (j === i ? { ...s, startHour: Number(e.target.value) } : s)),
                  )
                }
              >
                {Array.from({ length: 24 }, (_, h) => (
                  <option key={h} value={h}>
                    {hour(h)}
                  </option>
                ))}
              </select>
              <span>–</span>
              <select
                aria-label="Līdz"
                className="select-small"
                value={slot.endHour}
                onChange={(e) =>
                  setAvailability(
                    availability.map((s, j) => (j === i ? { ...s, endHour: Number(e.target.value) } : s)),
                  )
                }
              >
                {Array.from({ length: 24 }, (_, h) => h + 1).map((h) => (
                  <option key={h} value={h}>
                    {hour(h)}
                  </option>
                ))}
              </select>
              <button
                type="button"
                className="btn btn-small btn-ghost"
                aria-label="Noņemt laiku"
                onClick={() => setAvailability(availability.filter((_, j) => j !== i))}
              >
                ✕
              </button>
            </div>
          ))}
          <button
            type="button"
            className="btn btn-small"
            onClick={() => setAvailability([...availability, { dayOfWeek: 1, startHour: 18, endHour: 21 }])}
          >
            + Pievienot laiku
          </button>
        </section>

        <div className="form-footer">
          <ErrorBox message={error} />
          <div className="actions">
            <button className="btn btn-primary" type="submit" disabled={saving}>
              {saving ? "Saglabā…" : "Saglabāt profilu"}
            </button>
            {savedAt && <span className="muted small">Saglabāts {savedAt.toLocaleTimeString("lv-LV")}</span>}
          </div>
        </div>
      </form>

      <section className="card top-gap">
        <h2>Mani dati (GDPR)</h2>
        <p className="muted small">
          Tu vari apskatīt, labot, lejupielādēt un dzēst visus par sevi glabātos datus.
        </p>
        <div className="actions">
          <button
            className="btn"
            onClick={() =>
              download("/profile/export", "groupmate-mani-dati.json").catch((e) => toast(errorMessage(e), "error"))
            }
          >
            Lejupielādēt manus datus (JSON)
          </button>
          <button className="btn btn-danger" onClick={notImplemented}>
            Dzēst kontu
          </button>
          <PrototypeTag>Konta dzēšana – tikai vizuāli</PrototypeTag>
        </div>
      </section>
    </>
  );
}
