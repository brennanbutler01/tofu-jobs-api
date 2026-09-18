"""Real HTTP/PostgreSQL checks for the private local recovery stack."""
import json
import unittest
from urllib.request import Request, urlopen
from urllib.error import HTTPError

BASE = "http://127.0.0.1:5212"
def request(path, method="GET", body=None, token=None):
    headers = {"Content-Type":"application/json"}
    if token: headers["Authorization"] = "Bearer " + token
    try:
        response = urlopen(Request(BASE+path, method=method, headers=headers, data=None if body is None else json.dumps(body).encode()), timeout=10)
    except HTTPError as error: response = error
    with response:
        raw=response.read()
        try: body=json.loads(raw)
        except ValueError: body=raw.decode()[:300]
        return response.status, body

class OwnershipTests(unittest.TestCase):
    def setUp(self):
        self.alice=request("/dev/token/alice")[1]["accessToken"]
        self.bob=request("/dev/token/bob")[1]["accessToken"]
        self.alice_subject, self.bob_subject = "demo-alice", "demo-bob"

    def test_owned_lifecycle_and_relationships(self):
        alice, bob = self.alice, self.bob
        records=[]
        self.assertEqual(request('/UserId/someone@example.invalid',token=alice)[0],404)
        def create(kind, data, token=alice):
            status, record=request('/'+kind, 'POST', {**data, "userId":"forged-owner"},token)
            self.assertEqual(status,201,record)
            self.assertEqual(record['userId'], self.alice_subject if token==alice else self.bob_subject)
            records.append((kind,record,token))
            return record
        try:
            company=create('Company', {'name':'Synthetic Company','jobs':[]})
            other=create('Company', {'name':'Other Company','jobs':[]},bob)
            listing=create('JobList', {'title':'Synthetic List','jobs':[]})
            job=create('Job', {'title':'Synthetic Engineer','location':'Remote','companyId':company['id'],'jobListId':listing['id'],'salary':200000})
            interview=create('Interview', {'round':1,'interviewType':0,'jobId':job['id'],'start':'2026-09-18T01:00:00Z','end':'2026-09-18T02:00:00Z'})
            activity=create('Activity', {'title':'Follow up','note':'Persist this note','jobId':job['id'],'activityCategory':1,'isCompleted':True})
            self.assertIsNotNone(activity['dateCompleted'])
            self.assertEqual(request('/Activity/'+str(activity['id']),token=alice)[1]['note'],'Persist this note')
            letter=create('CoverLetter', {'title':'Synthetic letter','url':'https://example.invalid/letter','jobId':job['id']})
            for kind,record,token in records:
                if token!=alice:continue
                path=f"/{kind}/{record['id']}"
                self.assertEqual(request(path,token=alice)[0],200)
                for method in ['GET','PUT','DELETE']:
                    self.assertEqual(request(path,method,record if method=='PUT' else None,bob)[0],404)
                self.assertEqual(request(path,'PUT',{**record,'id':record['id']+100000},alice)[0],400)
                status,updated=request(path,'PUT',{**record,'userId':'demo-bob'},alice)
                self.assertEqual(status,200,updated)
                self.assertEqual(updated['userId'],self.alice_subject)
                self.assertNotIn(record['id'],[x['id'] for x in request('/'+kind,token=bob)[1]])
            self.assertEqual(request('/Job/'+str(job['id']),'PUT',{**job,'companyId':other['id']},alice)[0],400)
            self.assertEqual(request('/Company/'+str(company['id']),'DELETE',token=alice)[0],409)
            self.assertEqual(request('/Job/'+str(job['id']),'DELETE',token=alice)[0],409)
        finally:
            for kind,record,token in reversed(records):
                self.assertEqual(request(f"/{kind}/{record['id']}",'DELETE',token=token)[0],200)

    def test_anonymous_access_is_rejected(self):
        for kind in ['Company','Job','JobList','Interview','CoverLetter','Activity']:
            self.assertEqual(request('/'+kind)[0],401)

if __name__=='__main__':unittest.main(verbosity=2)
