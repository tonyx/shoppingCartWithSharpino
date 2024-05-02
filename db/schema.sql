SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: public; Type: SCHEMA; Schema: -; Owner: -
--

-- *not* creating schema, since initdb creates it


--
-- Name: insert_01_cart_aggregate_event_and_return_id(bytea, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_01_cart_aggregate_event_and_return_id(event_in bytea, aggregate_id uuid, aggregate_state_id uuid) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
inserted_id integer;
    event_id integer;
BEGIN
    event_id := insert_01_cart_event_and_return_id(event_in, aggregate_id, aggregate_state_id);

INSERT INTO aggregate_events_01_cart(aggregate_id, event_id, aggregate_state_id )
VALUES(aggregate_id, event_id, aggregate_state_id) RETURNING id INTO inserted_id;
return event_id;
END;
$$;


--
-- Name: insert_01_cart_event_and_return_id(bytea, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_01_cart_event_and_return_id(event_in bytea, aggregate_id uuid, aggregate_state_id uuid) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
inserted_id integer;
BEGIN
INSERT INTO events_01_cart(event, aggregate_id, timestamp)
VALUES(event_in::bytea, aggregate_id,  (now() at time zone 'utc')) RETURNING id INTO inserted_id;
return inserted_id;
END;
$$;


--
-- Name: insert_01_good_aggregate_event_and_return_id(bytea, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_01_good_aggregate_event_and_return_id(event_in bytea, aggregate_id uuid, aggregate_state_id uuid) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
inserted_id integer;
    event_id integer;
BEGIN
    event_id := insert_01_good_event_and_return_id(event_in, aggregate_id, aggregate_state_id);

INSERT INTO aggregate_events_01_good(aggregate_id, event_id, aggregate_state_id )
VALUES(aggregate_id, event_id, aggregate_state_id) RETURNING id INTO inserted_id;
return event_id;
END;
$$;


--
-- Name: insert_01_good_event_and_return_id(bytea, uuid, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_01_good_event_and_return_id(event_in bytea, aggregate_id uuid, aggregate_state_id uuid) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
inserted_id integer;
BEGIN
INSERT INTO events_01_good(event, aggregate_id, timestamp)
VALUES(event_in::bytea, aggregate_id, (now() at time zone 'utc')) RETURNING id INTO inserted_id;
return inserted_id;
END;
$$;


--
-- Name: insert_01_goodscontainer_event_and_return_id(bytea, uuid); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.insert_01_goodscontainer_event_and_return_id(event_in bytea, context_state_id uuid) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    inserted_id integer;
BEGIN
    INSERT INTO events_01_goodsContainer(event, timestamp, context_state_id)
    VALUES(event_in::bytea, (now() at time zone 'utc'), context_state_id) RETURNING id INTO inserted_id;
    return inserted_id;

END;
$$;


--
-- Name: set_classic_optimistic_lock_01_cart(); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.set_classic_optimistic_lock_01_cart()
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'aggregate_events_01_cart_aggregate_id_state_id_unique') THEN
ALTER TABLE aggregate_events_01_cart
    ADD CONSTRAINT aggregate_events_01_cart_aggregate_id_state_id_unique UNIQUE (aggregate_state_id);
END IF;
END;
$$;


--
-- Name: set_classic_optimistic_lock_01_good(); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.set_classic_optimistic_lock_01_good()
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'aggregate_events_01_good_aggregate_id_state_id_unique') THEN
ALTER TABLE aggregate_events_01_good
    ADD CONSTRAINT aggregate_events_01_good_aggregate_id_state_id_unique UNIQUE (aggregate_state_id);
END IF;
END;
$$;


--
-- Name: set_classic_optimistic_lock_01_goodscontainer(); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.set_classic_optimistic_lock_01_goodscontainer()
    LANGUAGE plpgsql
    AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'context_events_01_goodsContainer_context_state_id_unique') THEN
ALTER TABLE events_01_goodsContainer
    ADD CONSTRAINT context_events_01_goodsContainer_context_state_id_unique UNIQUE (context_state_id);
END IF;
END;
$$;


--
-- Name: un_set_classic_optimistic_lock_01_cart(); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.un_set_classic_optimistic_lock_01_cart()
    LANGUAGE plpgsql
    AS $$
BEGIN
    ALTER TABLE aggregate_events_01_cart
    DROP CONSTRAINT IF EXISTS aggregate_events_01_cart_aggregate_id_state_id_unique;
    -- You can have more SQL statements as needed
END;
$$;


--
-- Name: un_set_classic_optimistic_lock_01_good(); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.un_set_classic_optimistic_lock_01_good()
    LANGUAGE plpgsql
    AS $$
BEGIN
    ALTER TABLE aggregate_events_01_good
    DROP CONSTRAINT IF EXISTS aggregate_events_01_good_aggregate_id_state_id_unique;
    -- You can have more SQL statements as needed
END;
$$;


--
-- Name: un_set_classic_optimistic_lockcontext_events_01_goodscontainer(); Type: PROCEDURE; Schema: public; Owner: -
--

CREATE PROCEDURE public.un_set_classic_optimistic_lockcontext_events_01_goodscontainer()
    LANGUAGE plpgsql
    AS $$
BEGIN
    ALTER TABLE eventscontext_events_01_goodsContainer
    DROP CONSTRAINT IF EXISTS context_eventscontext_events_01_goodsContainer_context_state_id_unique;
END;
$$;


--
-- Name: aggregate_events_01_cart_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.aggregate_events_01_cart_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: aggregate_events_01_cart; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.aggregate_events_01_cart (
    id integer DEFAULT nextval('public.aggregate_events_01_cart_id_seq'::regclass) NOT NULL,
    aggregate_id uuid NOT NULL,
    aggregate_state_id uuid,
    event_id integer
);


--
-- Name: aggregate_events_01_good_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.aggregate_events_01_good_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: aggregate_events_01_good; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.aggregate_events_01_good (
    id integer DEFAULT nextval('public.aggregate_events_01_good_id_seq'::regclass) NOT NULL,
    aggregate_id uuid NOT NULL,
    aggregate_state_id uuid,
    event_id integer
);


--
-- Name: events_01_cart; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.events_01_cart (
    id integer NOT NULL,
    aggregate_id uuid NOT NULL,
    event bytea NOT NULL,
    published boolean DEFAULT false NOT NULL,
    kafkaoffset bigint,
    kafkapartition integer,
    "timestamp" timestamp without time zone NOT NULL
);


--
-- Name: events_01_cart_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.events_01_cart ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.events_01_cart_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: events_01_good; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.events_01_good (
    id integer NOT NULL,
    aggregate_id uuid NOT NULL,
    event bytea NOT NULL,
    published boolean DEFAULT false NOT NULL,
    kafkaoffset bigint,
    kafkapartition integer,
    "timestamp" timestamp without time zone NOT NULL
);


--
-- Name: events_01_good_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.events_01_good ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.events_01_good_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: events_01_goodscontainer; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.events_01_goodscontainer (
    id integer NOT NULL,
    event bytea NOT NULL,
    published boolean DEFAULT false NOT NULL,
    kafkaoffset bigint,
    kafkapartition integer,
    context_state_id uuid NOT NULL,
    "timestamp" timestamp without time zone NOT NULL
);


--
-- Name: events_01_goodscontainer_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

ALTER TABLE public.events_01_goodscontainer ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.events_01_goodscontainer_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: schema_migrations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.schema_migrations (
    version character varying(128) NOT NULL
);


--
-- Name: snapshots_01_cart_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.snapshots_01_cart_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: snapshots_01_cart; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.snapshots_01_cart (
    id integer DEFAULT nextval('public.snapshots_01_cart_id_seq'::regclass) NOT NULL,
    snapshot bytea NOT NULL,
    event_id integer,
    aggregate_id uuid NOT NULL,
    aggregate_state_id uuid,
    "timestamp" timestamp without time zone NOT NULL
);


--
-- Name: snapshots_01_good_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.snapshots_01_good_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: snapshots_01_good; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.snapshots_01_good (
    id integer DEFAULT nextval('public.snapshots_01_good_id_seq'::regclass) NOT NULL,
    snapshot bytea NOT NULL,
    event_id integer,
    aggregate_id uuid NOT NULL,
    aggregate_state_id uuid,
    "timestamp" timestamp without time zone NOT NULL
);


--
-- Name: snapshots_01_goodscontainer_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.snapshots_01_goodscontainer_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: snapshots_01_goodscontainer; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.snapshots_01_goodscontainer (
    id integer DEFAULT nextval('public.snapshots_01_goodscontainer_id_seq'::regclass) NOT NULL,
    snapshot bytea NOT NULL,
    event_id integer NOT NULL,
    "timestamp" timestamp without time zone NOT NULL
);


--
-- Name: aggregate_events_01_cart aggregate_events_01_cart_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.aggregate_events_01_cart
    ADD CONSTRAINT aggregate_events_01_cart_pkey PRIMARY KEY (id);


--
-- Name: aggregate_events_01_good aggregate_events_01_good_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.aggregate_events_01_good
    ADD CONSTRAINT aggregate_events_01_good_pkey PRIMARY KEY (id);


--
-- Name: events_01_cart events_cart_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.events_01_cart
    ADD CONSTRAINT events_cart_pkey PRIMARY KEY (id);


--
-- Name: events_01_good events_good_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.events_01_good
    ADD CONSTRAINT events_good_pkey PRIMARY KEY (id);


--
-- Name: events_01_goodscontainer events_goodscontainer_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.events_01_goodscontainer
    ADD CONSTRAINT events_goodscontainer_pkey PRIMARY KEY (id);


--
-- Name: schema_migrations schema_migrations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.schema_migrations
    ADD CONSTRAINT schema_migrations_pkey PRIMARY KEY (version);


--
-- Name: snapshots_01_cart snapshots_cart_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.snapshots_01_cart
    ADD CONSTRAINT snapshots_cart_pkey PRIMARY KEY (id);


--
-- Name: snapshots_01_good snapshots_good_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.snapshots_01_good
    ADD CONSTRAINT snapshots_good_pkey PRIMARY KEY (id);


--
-- Name: snapshots_01_goodscontainer snapshots_goodscontainer_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.snapshots_01_goodscontainer
    ADD CONSTRAINT snapshots_goodscontainer_pkey PRIMARY KEY (id);


--
-- Name: aggregate_events_01_good aggregate_events_01_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.aggregate_events_01_good
    ADD CONSTRAINT aggregate_events_01_fk FOREIGN KEY (event_id) REFERENCES public.events_01_good(id) MATCH FULL ON DELETE CASCADE;


--
-- Name: aggregate_events_01_cart aggregate_events_01_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.aggregate_events_01_cart
    ADD CONSTRAINT aggregate_events_01_fk FOREIGN KEY (event_id) REFERENCES public.events_01_cart(id) MATCH FULL ON DELETE CASCADE;


--
-- Name: snapshots_01_cart event_01_cart_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.snapshots_01_cart
    ADD CONSTRAINT event_01_cart_fk FOREIGN KEY (event_id) REFERENCES public.events_01_cart(id) MATCH FULL ON DELETE CASCADE;


--
-- Name: snapshots_01_good event_01_good_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.snapshots_01_good
    ADD CONSTRAINT event_01_good_fk FOREIGN KEY (event_id) REFERENCES public.events_01_good(id) MATCH FULL ON DELETE CASCADE;


--
-- Name: snapshots_01_goodscontainer event_01_goodscontainer_fk; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.snapshots_01_goodscontainer
    ADD CONSTRAINT event_01_goodscontainer_fk FOREIGN KEY (event_id) REFERENCES public.events_01_goodscontainer(id) MATCH FULL ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--


--
-- Dbmate schema migrations
--

INSERT INTO public.schema_migrations (version) VALUES
    ('20240426152015'),
    ('20240426152449'),
    ('20240426152543'),
    ('20240426155417');
