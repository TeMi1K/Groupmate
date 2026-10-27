import { Link } from "react-router-dom";
import { PrototypeTag } from "../components";
import { useNotImplemented } from "../context";

/** Reģistrācija (prasība 2.1.) – prototipā tikai vizuāli. */
export default function RegisterPage() {
  const notImplemented = useNotImplemented();

  return (
    <div className="login-page">
      <div className="login-brand">
        <h1>
          Group<span>Mate</span>
        </h1>
      </div>
      <section className="card narrow">
        <h2>
          Reģistrācija <PrototypeTag />
        </h2>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            notImplemented();
          }}
        >
          <div className="row">
            <label>
              Vārds
              <input required />
            </label>
            <label>
              Uzvārds
              <input required />
            </label>
          </div>
          <label>
            E-pasts
            <input type="email" required />
          </label>
          <label>
            Parole
            <input type="password" required minLength={8} />
          </label>
          <label className="checkbox">
            <input type="checkbox" required />
            <span>
              Piekrītu savu personas datu apstrādei komandu veidošanas nolūkā (GDPR). Netiek vākts personas kods,
              adrese vai telefona numurs.
            </span>
          </label>
          <button className="btn btn-primary btn-block" type="submit">
            Izveidot kontu
          </button>
        </form>
        <p className="small">
          Jau ir konts? <Link to="/login">Pieslēgties</Link>
        </p>
      </section>
    </div>
  );
}
